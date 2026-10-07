import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/theme/app_theme.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';
import 'package:smartsheep_mobile/features/sources/sources_screen.dart';
import 'package:smartsheep_mobile/features/sources/source_video_detail_screen.dart';

class _TestAuth extends AuthController {
  @override
  Future<AuthSession?> build() async => const AuthSession(
    token: 'test-token',
    username: 'operator',
    profile: {'FullName': 'Farm Operator'},
  );
}

class _SourcesApi extends Fake implements ApiClient {
  _SourcesApi({bool empty = false}) {
    if (empty) return;
    farms = [
      {'Id': 'farm-a', 'Name': 'Alpha Farm', 'Code': 'F-A', 'IsActive': true},
      {'Id': 'farm-b', 'Name': 'Beta Farm', 'Code': 'F-B', 'IsActive': true},
    ];
    barns = [
      {
        'Id': 'barn-a',
        'Name': 'North Barn',
        'Code': 'B-NORTH',
        'FkFarmId': 'farm-a',
        'IsActive': true,
      },
      {
        'Id': 'barn-empty',
        'Name': 'Empty Barn',
        'Code': 'B-EMPTY',
        'FkFarmId': 'farm-a',
        'IsActive': true,
      },
      {
        'Id': 'barn-b',
        'Name': 'West Barn',
        'Code': 'B-WEST',
        'FkFarmId': 'farm-b',
        'IsActive': true,
      },
    ];
    sources = [
      {
        'Id': 'source-a1',
        'Code': 'SOURCE-A-ONLINE',
        'FkBarnId': 'barn-a',
        'IsOnline': true,
        'IsActive': true,
        'SourceVideoUrl': 'rtsp://example.invalid/online',
      },
      {
        'Id': 'source-a2',
        'Code': 'SOURCE-A-OFFLINE',
        'FkBarnId': 'barn-a',
        'IsOnline': false,
        'IsActive': true,
      },
      {
        'Id': 'source-b',
        'Code': 'SOURCE-B-OFFLINE',
        'FkBarnId': 'barn-b',
        'IsOnline': false,
        'IsActive': true,
      },
    ];
  }

  List<Map<String, Object?>> sources = [];
  List<Map<String, Object?>> barns = [];
  List<Map<String, Object?>> farms = [];
  final requests = <String>[];
  String? failingPath;
  Completer<Object?>? pendingSources;

  @override
  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    requests.add(path);
    if (path == failingPath) throw StateError('API unavailable');
    return switch (path) {
      '/api/SourceVideo/All' => pendingSources?.future ?? sources,
      '/api/Barn/All' => barns,
      '/api/Farm/All' => farms,
      _ => throw StateError('Unexpected API request: $path'),
    };
  }

  int calls(String path) => requests.where((request) => request == path).length;
}

const _paths = ['/api/SourceVideo/All', '/api/Barn/All', '/api/Farm/All'];

Future<void> _pumpSources(
  WidgetTester tester,
  _SourcesApi api, {
  Size size = const Size(393, 2000),
  double textScale = 1,
  bool settle = true,
}) async {
  tester.view.physicalSize = size;
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        apiClientProvider.overrideWithValue(api),
        authControllerProvider.overrideWith(_TestAuth.new),
        headerProfileImageProvider.overrideWith((_) async => null),
        unreadNotificationCountProvider.overrideWithValue(0),
      ],
      child: MaterialApp(
        theme: AppTheme.light,
        builder: (context, child) => MediaQuery(
          data: MediaQuery.of(
            context,
          ).copyWith(textScaler: TextScaler.linear(textScale)),
          child: child!,
        ),
        home: const SourcesScreen(),
      ),
    ),
  );
  if (settle) {
    await tester.pumpAndSettle();
  } else {
    await tester.pump();
  }
}

Finder _key(String value) => find.byKey(ValueKey(value));

void _expectChild(String parent, String child) {
  expect(
    find.descendant(of: _key(parent), matching: _key(child)),
    findsOneWidget,
  );
}

void _expectStatus(String source, bool online) {
  expect(
    find.descendant(
      of: _key('sources-video-$source'),
      matching: find.text(online ? '● Online' : '● Offline'),
    ),
    findsOneWidget,
  );
}

void _expectCount(String filter, String count) {
  expect(
    find.descendant(
      of: _key('sources-filter-$filter'),
      matching: find.text(count),
    ),
    findsOneWidget,
  );
}

void _expectNoDemo() {
  expect(find.textContaining('Barn A Camera'), findsNothing);
  expect(find.textContaining('Last checked'), findsNothing);
  expect(find.textContaining('min ago'), findsNothing);
  expect(find.byIcon(Icons.play_arrow), findsNothing);
  expect(find.byIcon(Icons.agriculture), findsNothing);
}

void main() {
  test(
    'preview accepts HTTP video URLs and validates YouTube hosts and IDs',
    () {
      for (final url in [
        'https://www.youtube.com/watch?v=pBFmbzw0cdw',
        'https://youtu.be/pBFmbzw0cdw?t=5',
        'https://m.youtube.com/shorts/pBFmbzw0cdw',
        'https://youtube.com/live/pBFmbzw0cdw',
        'https://www.youtube-nocookie.com/embed/pBFmbzw0cdw',
      ]) {
        expect(sourceYoutubeId(sourceVideoUri(url)!), 'pBFmbzw0cdw');
      }
      expect(
        sourceYoutubeId(
          Uri.parse('https://youtube.com.evil.invalid/watch?v=pBFmbzw0cdw'),
        ),
        isNull,
      );
      expect(
        sourceYoutubeId(Uri.parse('https://youtube.com/watch?v=bad')),
        isNull,
      );
      expect(sourceVideoUri('https://example.invalid/live.m3u8'), isNotNull);
      expect(sourceVideoUri('https://example.invalid/video.mp4'), isNotNull);
      for (final url in [
        null,
        '',
        'invalid',
        'javascript:alert(1)',
        'file:///video.mp4',
        'rtsp://camera/live',
        'https://user:password@camera/live',
      ]) {
        expect(sourceVideoUri(url), isNull);
      }
    },
  );

  testWidgets(
    'tapping a source opens its detail with exact farm, barn and status',
    (tester) async {
      await _pumpSources(tester, _SourcesApi());
      await tester.tap(_key('sources-video-source-a1'));
      await tester.pumpAndSettle();
      expect(find.byType(SourceVideoDetailScreen), findsOneWidget);
      expect(find.text('Video Detail'), findsOneWidget);
      expect(find.text('Alpha Farm'), findsOneWidget);
      expect(find.text('North Barn'), findsOneWidget);
      expect(find.text('B-NORTH'), findsOneWidget);
      expect(find.text('Online'), findsOneWidget);
      expect(find.text('Preview unavailable'), findsOneWidget);
      expect(find.textContaining('HTTP(S)'), findsOneWidget);
      await tester.pageBack();
      await tester.pumpAndSettle();
      expect(_key('sources-video-source-a1'), findsOneWidget);
    },
  );

  testWidgets(
    'offline detail with missing URL remains readable on a narrow screen',
    (tester) async {
      await _pumpSources(
        tester,
        _SourcesApi(),
        size: const Size(320, 900),
        textScale: 2,
      );
      await tester.ensureVisible(_key('sources-video-source-a2'));
      await tester.pumpAndSettle();
      await tester.tap(_key('sources-video-source-a2'));
      await tester.pumpAndSettle();
      expect(
        find.text('No video URL is configured for this source.'),
        findsOneWidget,
      );
      expect(find.textContaining('marked offline'), findsOneWidget);
      expect(find.text('Open original'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('joins API farms to their barns and source code/status rows', (
    tester,
  ) async {
    final api = _SourcesApi();
    await _pumpSources(tester, api);

    for (final path in _paths) {
      expect(api.calls(path), 1);
    }
    expect(find.text('Barns grouped by farm'), findsOneWidget);
    expect(find.text('2 farms · 3 barns · 3 sources'), findsOneWidget);
    _expectChild('sources-farm-farm-a', 'sources-barn-barn-a');
    _expectChild('sources-farm-farm-a', 'sources-barn-barn-empty');
    _expectChild('sources-farm-farm-b', 'sources-barn-barn-b');
    _expectChild('sources-barn-barn-a', 'sources-video-source-a1');
    _expectChild('sources-barn-barn-a', 'sources-video-source-a2');
    _expectChild('sources-barn-barn-b', 'sources-video-source-b');
    expect(
      find.descendant(
        of: _key('sources-farm-farm-a'),
        matching: _key('sources-video-source-b'),
      ),
      findsNothing,
    );
    for (final text in [
      'Alpha Farm',
      'Beta Farm',
      'North Barn',
      'West Barn',
      'B-NORTH',
      'SOURCE-A-ONLINE',
      'SOURCE-A-OFFLINE',
      'SOURCE-B-OFFLINE',
    ]) {
      expect(find.text(text), findsOneWidget);
    }
    _expectStatus('source-a1', true);
    _expectStatus('source-a2', false);
    _expectStatus('source-b', false);
    _expectCount('all', '3');
    _expectCount('online', '1');
    _expectCount('offline', '2');
    _expectNoDemo();
    expect(tester.takeException(), isNull);
  });

  testWidgets('All keeps real empty barns without inventing offline sources', (
    tester,
  ) async {
    await _pumpSources(tester, _SourcesApi());
    final barn = _key('sources-barn-barn-empty');
    expect(
      find.descendant(of: barn, matching: find.text('Empty Barn')),
      findsOneWidget,
    );
    expect(
      find.descendant(of: barn, matching: find.text('No sources configured')),
      findsOneWidget,
    );
    expect(
      find.descendant(of: barn, matching: find.text('● Offline')),
      findsNothing,
    );
    _expectCount('offline', '2');
  });

  testWidgets(
    'Online and Offline controls filter grouped rows and All restores them',
    (tester) async {
      final api = _SourcesApi();
      await _pumpSources(tester, api);
      await tester.tap(_key('sources-filter-online'));
      await tester.pumpAndSettle();
      expect(_key('sources-video-source-a1'), findsOneWidget);
      expect(_key('sources-video-source-a2'), findsNothing);
      expect(_key('sources-farm-farm-b'), findsNothing);
      expect(_key('sources-barn-barn-empty'), findsNothing);
      _expectCount('all', '3');
      _expectCount('online', '1');
      _expectCount('offline', '2');

      await tester.tap(_key('sources-filter-offline'));
      await tester.pumpAndSettle();
      expect(_key('sources-video-source-a1'), findsNothing);
      _expectChild('sources-farm-farm-a', 'sources-video-source-a2');
      _expectChild('sources-farm-farm-b', 'sources-video-source-b');
      expect(_key('sources-barn-barn-empty'), findsNothing);
      expect(find.text('● Offline'), findsNWidgets(2));

      await tester.tap(_key('sources-filter-all'));
      await tester.pumpAndSettle();
      expect(_key('sources-barn-barn-empty'), findsOneWidget);
      expect(find.text('● Online'), findsOneWidget);
      expect(find.text('● Offline'), findsNWidgets(2));
      expect(api.requests, hasLength(3));
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'empty API results show honest no-data instead of demo camera cards',
    (tester) async {
      await _pumpSources(tester, _SourcesApi(empty: true));
      expect(find.text('No sources'), findsOneWidget);
      expect(find.text('No source videos are configured yet.'), findsOneWidget);
      _expectCount('all', '0');
      _expectCount('online', '0');
      _expectCount('offline', '0');
      expect(_key('sources-list'), findsNothing);
      _expectNoDemo();
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('empty availability filters explain why no rows match', (
    tester,
  ) async {
    final api = _SourcesApi();
    api.sources = [api.sources.first];
    await _pumpSources(tester, api);
    await tester.tap(_key('sources-filter-offline'));
    await tester.pumpAndSettle();
    expect(find.text('No offline sources'), findsOneWidget);
    expect(find.text('All configured sources are online.'), findsOneWidget);
    _expectCount('all', '1');

    api.sources.single['IsOnline'] = false;
    await tester
        .widget<RefreshIndicator>(find.byType(RefreshIndicator))
        .onRefresh();
    await tester.pumpAndSettle();
    await tester.tap(_key('sources-filter-online'));
    await tester.pumpAndSettle();
    expect(find.text('No online sources'), findsOneWidget);
    expect(
      find.text('No configured sources are currently online.'),
      findsOneWidget,
    );
    _expectNoDemo();
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    'loading waits for all API lists and failure retries the actual endpoints',
    (tester) async {
      final api = _SourcesApi();
      api.pendingSources = Completer<Object?>();
      await _pumpSources(tester, api, settle: false);
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(_key('sources-farm-farm-a'), findsNothing);
      _expectNoDemo();
      api.pendingSources!.completeError(
        StateError('API temporarily unavailable'),
      );
      await tester.pumpAndSettle();
      expect(find.text('Unable to load sources'), findsOneWidget);
      expect(_key('sources-retry'), findsOneWidget);
      expect(find.text('No sources'), findsNothing);
      _expectNoDemo();

      api.pendingSources = null;
      await tester.tap(_key('sources-retry'));
      await tester.pumpAndSettle();
      for (final path in _paths) {
        expect(api.calls(path), 2);
      }
      expect(_key('sources-farm-farm-a'), findsOneWidget);
      expect(find.text('Unable to load sources'), findsNothing);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'master endpoint failures never render partially joined farm data',
    (tester) async {
      final api = _SourcesApi()..failingPath = '/api/Barn/All';
      await _pumpSources(tester, api);
      expect(find.text('Unable to load sources'), findsOneWidget);
      expect(find.text('Alpha Farm'), findsNothing);
      expect(find.text('SOURCE-A-ONLINE'), findsNothing);
      _expectNoDemo();
      api.failingPath = null;
      await tester.tap(_key('sources-retry'));
      await tester.pumpAndSettle();
      expect(find.text('Alpha Farm'), findsOneWidget);
      expect(find.text('SOURCE-A-ONLINE'), findsOneWidget);
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets(
    'pull refresh updates authoritative names codes status and totals',
    (tester) async {
      final api = _SourcesApi();
      await _pumpSources(tester, api);
      api.farms.first['Name'] = 'Renamed Farm';
      api.barns.first['Name'] = 'Renamed Barn';
      api.sources.first['Code'] = 'RENAMED-SOURCE';
      api.sources.first['IsOnline'] = false;
      expect(_key('sources-refresh'), findsNothing);
      await tester
          .widget<RefreshIndicator>(find.byType(RefreshIndicator))
          .onRefresh();
      await tester.pumpAndSettle();
      for (final path in _paths) {
        expect(api.calls(path), 2);
      }
      expect(find.text('Renamed Farm'), findsOneWidget);
      expect(find.text('Renamed Barn'), findsOneWidget);
      expect(find.text('RENAMED-SOURCE'), findsOneWidget);
      expect(find.text('Alpha Farm'), findsNothing);
      expect(find.text('SOURCE-A-ONLINE'), findsNothing);
      _expectStatus('source-a1', false);
      _expectCount('online', '0');
      _expectCount('offline', '3');
      expect(tester.takeException(), isNull);
    },
  );

  testWidgets('pull-to-refresh reloads all three endpoint lists', (
    tester,
  ) async {
    final api = _SourcesApi();
    await _pumpSources(tester, api, size: const Size(393, 852));
    api.sources.removeLast();
    await tester.drag(find.byType(Scrollable).last, const Offset(0, 350));
    await tester.pumpAndSettle();
    for (final path in _paths) {
      expect(api.calls(path), 2);
    }
    _expectCount('all', '2');
    expect(tester.takeException(), isNull);
  });

  testWidgets(
    '320px at 2x text keeps long farm barn and source names usable without overflow',
    (tester) async {
      final api = _SourcesApi();
      api.farms = [api.farms.first];
      api.barns = [api.barns.first];
      api.sources = [api.sources.first];
      api.farms.single['Name'] =
          'A very long real farm name with multiple descriptive words';
      api.barns.single['Name'] =
          'North side livestock nursery barn with an extended official name';
      api.barns.single['Code'] = 'BARN-CODE-WITH-A-VERY-LONG-SUFFIX-2026';
      api.sources.single['Code'] =
          'SOURCE-CODE-WITH-A-VERY-LONG-SUFFIX-00000001';
      await _pumpSources(tester, api, size: const Size(320, 568), textScale: 2);
      expect(_key('sources-filter-all').hitTestable(), findsOneWidget);
      expect(_key('sources-filter-online').hitTestable(), findsOneWidget);
      expect(_key('sources-filter-offline').hitTestable(), findsOneWidget);
      expect(tester.takeException(), isNull);
      await tester.scrollUntilVisible(
        _key('sources-video-source-a1'),
        180,
        scrollable: find.byType(Scrollable).last,
        maxScrolls: 40,
      );
      await tester.pumpAndSettle();
      expect(_key('sources-video-source-a1').hitTestable(), findsOneWidget);
      _expectStatus('source-a1', true);
      expect(tester.takeException(), isNull);
    },
  );
}
