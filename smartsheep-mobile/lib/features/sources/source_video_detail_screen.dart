import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:video_player/video_player.dart';
import 'package:youtube_player_iframe/youtube_player_iframe.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';
import 'sources_data.dart';

Uri? sourceVideoUri(String? value) {
  final uri = Uri.tryParse(value?.trim() ?? '');
  if (uri == null ||
      !['https', 'http'].contains(uri.scheme) ||
      uri.host.isEmpty ||
      uri.userInfo.isNotEmpty) {
    return null;
  }
  return uri;
}

String? sourceYoutubeId(Uri uri) {
  final host = uri.host.toLowerCase();
  String? id;
  if (host == 'youtu.be') {
    id = uri.pathSegments.firstOrNull;
  } else if ([
    'youtube.com',
    'www.youtube.com',
    'm.youtube.com',
    'youtube-nocookie.com',
    'www.youtube-nocookie.com',
  ].contains(host)) {
    if (uri.path == '/watch') {
      id = uri.queryParameters['v'];
    } else if (uri.pathSegments.length == 2 &&
        ['embed', 'shorts', 'live'].contains(uri.pathSegments.first)) {
      id = uri.pathSegments[1];
    }
  }
  return id != null && RegExp(r'^[a-zA-Z0-9_-]{11}$').hasMatch(id) ? id : null;
}

class SourceVideoDetailScreen extends StatefulWidget {
  const SourceVideoDetailScreen({
    super.key,
    required this.source,
    required this.farm,
    required this.barn,
  });

  final SourceVideoItem source;
  final SourcesFarmGroup farm;
  final SourcesBarnGroup barn;

  @override
  State<SourceVideoDetailScreen> createState() =>
      _SourceVideoDetailScreenState();
}

class _SourceVideoDetailScreenState extends State<SourceVideoDetailScreen> {
  YoutubePlayerController? _youtube;
  VideoPlayerController? _video;
  StreamSubscription<YoutubePlayerValue>? _youtubeEvents;
  Timer? _timeout;
  late final AppLifecycleListener _lifecycle;
  String? _error;
  bool _loading = true;
  int _generation = 0;

  Uri? get _uri => sourceVideoUri(widget.source.sourceVideoUrl);

  @override
  void initState() {
    super.initState();
    _lifecycle = AppLifecycleListener(onInactive: _pause);
    _load();
  }

  void _pause() {
    _video?.pause();
    if (_youtube?.value.playerState == PlayerState.playing) {
      _youtube?.pauseVideo();
    }
  }

  void _release() {
    _timeout?.cancel();
    _youtubeEvents?.cancel();
    _youtube?.close();
    _video?.dispose();
    _youtube = null;
    _video = null;
  }

  Future<void> _load() async {
    final generation = ++_generation;
    _release();
    _error = null;
    _loading = true;
    final uri = _uri;
    if (uri == null) {
      _loading = false;
      _error = widget.source.sourceVideoUrl?.trim().isNotEmpty == true
          ? 'This source needs an HTTP(S) video or HLS URL for an in-app preview.'
          : 'No video URL is configured for this source.';
      return;
    }
    _timeout = Timer(const Duration(seconds: 25), () {
      if (!mounted || generation != _generation || !_loading) return;
      setState(() {
        _loading = false;
        _error = 'The video took too long to load. Please try again.';
      });
    });
    try {
      final youtubeId = sourceYoutubeId(uri);
      if (youtubeId != null) {
        _youtube = YoutubePlayerController.fromVideoId(
          videoId: youtubeId,
          autoPlay: false,
          params: YoutubePlayerParams(
            showControls: true,
            showFullscreenButton: true,
            // Identify this app to YouTube rather than spoofing another site.
            origin: defaultTargetPlatform == TargetPlatform.android
                ? 'https://com.pertamina.smartsheep_mobile'
                : 'https://com.pertamina.smartsheepMobile',
          ),
        );
        _youtubeEvents = _youtube!.listen((value) {
          if (!mounted || generation != _generation) return;
          if (value.hasError || value.playerState != PlayerState.unknown) {
            _timeout?.cancel();
            setState(() {
              _loading = false;
              _error = value.hasError
                  ? 'This video cannot be played here. Please try again or check the source.'
                  : null;
            });
          }
        });
      } else {
        final controller = VideoPlayerController.networkUrl(uri);
        _video = controller;
        await controller.initialize().timeout(const Duration(seconds: 25));
        if (!mounted || generation != _generation) return;
        _timeout?.cancel();
        controller.addListener(() {
          if (!mounted || generation != _generation) return;
          if (controller.value.hasError && _error == null) {
            setState(
              () => _error =
                  'Unable to play this video. Please check the source or try again.',
            );
          }
        });
        setState(() => _loading = false);
      }
    } catch (_) {
      if (!mounted || generation != _generation) return;
      _timeout?.cancel();
      setState(() {
        _loading = false;
        _error =
            'Unable to load this video. Please check the source or try again.';
      });
    }
  }

  @override
  void dispose() {
    _generation++;
    _lifecycle.dispose();
    _release();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final source = widget.source;
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: 'Video Detail',
            subtitle: source.code.isEmpty ? 'Source preview' : source.code,
            showBackButton: true,
          ),
          Expanded(
            child: SafeArea(
              top: false,
              child: SingleChildScrollView(
                padding: const EdgeInsets.fromLTRB(14, 16, 14, 24),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    ClipRRect(
                      borderRadius: BorderRadius.circular(14),
                      child: _player(),
                    ),
                    const SizedBox(height: 12),
                    if (!source.isOnline) ...[
                      const Text(
                        'This source is marked offline. Playback may be unavailable.',
                        style: TextStyle(color: AppColors.danger),
                      ),
                      const SizedBox(height: 12),
                    ],
                    if (_uri != null)
                      Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children: [
                          OutlinedButton.icon(
                            onPressed: () => setState(() {
                              _load();
                            }),
                            icon: const Icon(Icons.replay),
                            label: const Text('Retry video'),
                          ),
                        ],
                      ),
                    const SizedBox(height: 12),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            const Row(
                              children: [
                                Icon(
                                  Icons.videocam_outlined,
                                  color: AppColors.tealDark,
                                ),
                                SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    'Source Information',
                                    style: TextStyle(
                                      fontWeight: FontWeight.w800,
                                      fontSize: 16,
                                    ),
                                  ),
                                ),
                              ],
                            ),
                            const Divider(height: 28),
                            _detail(
                              'Source',
                              source.code.isEmpty
                                  ? 'Unnamed source'
                                  : source.code,
                            ),
                            _detail('Farm', widget.farm.name),
                            _detail('Barn', widget.barn.name),
                            if (widget.barn.code?.isNotEmpty == true)
                              _detail('Barn Code', widget.barn.code!),
                            _detail(
                              'Status',
                              source.isOnline ? 'Online' : 'Offline',
                              color: source.isOnline
                                  ? AppColors.success
                                  : AppColors.danger,
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _detail(String label, String value, {Color? color}) => Padding(
    padding: const EdgeInsets.only(bottom: 12),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: const TextStyle(fontSize: 12, color: AppColors.muted),
        ),
        const SizedBox(height: 3),
        Text(
          value,
          style: TextStyle(
            fontWeight: FontWeight.w600,
            color: color ?? AppColors.ink,
          ),
        ),
      ],
    ),
  );

  Widget _player() {
    if (_error != null) {
      return Container(
        constraints: const BoxConstraints(minHeight: 210),
        padding: const EdgeInsets.all(24),
        color: const Color(0xFFE5F6F4),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            const Icon(
              Icons.videocam_off_outlined,
              size: 40,
              color: AppColors.tealDark,
            ),
            const SizedBox(height: 12),
            const Text(
              'Preview unavailable',
              style: TextStyle(fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 8),
            Text(_error!, textAlign: TextAlign.center),
          ],
        ),
      );
    }
    if (_youtube != null) {
      return Column(
        children: [
          LayoutBuilder(
            builder: (context, constraints) => YoutubePlayer(
              key: ValueKey(_generation),
              controller: _youtube!,
              aspectRatio:
                  constraints.maxWidth /
                  math.max(200, constraints.maxWidth * 9 / 16),
              autoFullScreen: false,
              enableFullScreenOnVerticalDrag: false,
            ),
          ),
          if (_loading) const LinearProgressIndicator(),
        ],
      );
    }
    final controller = _video;
    if (_loading || controller == null) {
      return const SizedBox(
        height: 210,
        child: Center(child: CircularProgressIndicator()),
      );
    }
    return ValueListenableBuilder<VideoPlayerValue>(
      valueListenable: controller,
      builder: (context, value, _) => ColoredBox(
        color: Colors.black,
        child: Column(
          children: [
            AspectRatio(
              aspectRatio: value.aspectRatio > 0 ? value.aspectRatio : 16 / 9,
              child: Stack(
                alignment: Alignment.center,
                children: [
                  VideoPlayer(controller),
                  if (value.isBuffering) const CircularProgressIndicator(),
                ],
              ),
            ),
            VideoProgressIndicator(
              controller,
              allowScrubbing: true,
              colors: const VideoProgressColors(playedColor: AppColors.teal),
            ),
            Row(
              children: [
                IconButton(
                  tooltip: value.isPlaying ? 'Pause' : 'Play',
                  color: Colors.white,
                  icon: Icon(value.isPlaying ? Icons.pause : Icons.play_arrow),
                  onPressed: () async {
                    if (value.isPlaying) {
                      await controller.pause();
                    } else {
                      if (value.isCompleted) {
                        await controller.seekTo(Duration.zero);
                      }
                      await controller.play();
                    }
                  },
                ),
                Expanded(
                  child: Text(
                    '${_duration(value.position)} / ${_duration(value.duration)}',
                    style: const TextStyle(color: Colors.white, fontSize: 12),
                  ),
                ),
                IconButton(
                  tooltip: value.volume == 0 ? 'Unmute' : 'Mute',
                  color: Colors.white,
                  icon: Icon(
                    value.volume == 0 ? Icons.volume_off : Icons.volume_up,
                  ),
                  onPressed: () =>
                      controller.setVolume(value.volume == 0 ? 1 : 0),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  String _duration(Duration value) =>
      '${value.inMinutes}:${(value.inSeconds % 60).toString().padLeft(2, '0')}';
}
