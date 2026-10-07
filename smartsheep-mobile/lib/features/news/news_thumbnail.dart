import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';

final newsThumbnailBytesProvider = FutureProvider.autoDispose
    .family<Uint8List, String>(
      (ref, path) => ref.watch(apiClientProvider).getBytes(path),
    );

class NewsThumbnail extends ConsumerWidget {
  const NewsThumbnail({
    super.key,
    required this.imagePath,
    this.width = 70,
    this.height = 58,
    this.fit = BoxFit.cover,
    this.borderRadius = const BorderRadius.all(Radius.circular(10)),
    this.semanticLabel,
  });

  final String? imagePath;
  final double? width;
  final double? height;
  final BoxFit fit;
  final BorderRadius borderRadius;
  final String? semanticLabel;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final path = imagePath?.trim() ?? '';
    final image = path.isEmpty
        ? const _ThumbnailFallback()
        : ref
              .watch(newsThumbnailBytesProvider(path))
              .when(
                loading: () => const _ThumbnailLoading(),
                error: (_, _) => const _ThumbnailFallback(),
                data: (bytes) => Image.memory(
                  bytes,
                  fit: fit,
                  gaplessPlayback: true,
                  semanticLabel: semanticLabel,
                  errorBuilder: (_, _, _) => const _ThumbnailFallback(),
                ),
              );

    return ClipRRect(
      borderRadius: borderRadius,
      child: SizedBox(
        width: width,
        height: height,
        child: ColoredBox(color: AppColors.mint, child: image),
      ),
    );
  }
}

class _ThumbnailLoading extends StatelessWidget {
  const _ThumbnailLoading();

  @override
  Widget build(BuildContext context) => const Center(
    child: SizedBox.square(
      dimension: 18,
      child: CircularProgressIndicator(strokeWidth: 2),
    ),
  );
}

class _ThumbnailFallback extends StatelessWidget {
  const _ThumbnailFallback();

  @override
  Widget build(BuildContext context) => const Center(
    child: Icon(Icons.newspaper_outlined, color: AppColors.deepGreen),
  );
}
