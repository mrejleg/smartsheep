import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/features/news/news_data.dart';
import 'package:smartsheep_mobile/features/news/news_detail_screen.dart';
import 'package:smartsheep_mobile/features/news/news_feed.dart';
import 'package:smartsheep_mobile/features/news/news_item.dart';
import 'package:smartsheep_mobile/features/news/news_screen.dart';
import 'package:smartsheep_mobile/features/news/news_thumbnail.dart';

void main() {
  testWidgets('news list shows publication hours and minutes from the API', (
    tester,
  ) async {
    await _pumpNews(
      tester,
      loadNews: () => [
        NewsItem.fromJson({
          'Id': 'timestamp-news',
          'Title': 'News with time',
          'StartDate': '2026-08-30T17:39:28',
          'DateCreated': '2026-09-01T08:00:00',
        }),
        NewsItem.fromJson({
          'Id': 'fallback-news',
          'Title': 'News with creation time',
          'DateCreated': '2026-08-29T09:05:00',
        }),
      ],
    );
    expect(find.text('30 Aug 2026 17:39'), findsOneWidget);
    expect(find.text('29 Aug 2026 09:05'), findsOneWidget);
    expect(tester.takeException(), isNull);
  });

  testWidgets('paginates twelve articles as 5, 5, 2 with disabled boundaries', (
    tester,
  ) async {
    final imagePaths = <String>{};
    await _pumpNews(
      tester,
      loadNews: () => _newsItems(12),
      imagePaths: imagePaths,
    );

    _expectPage(tester, page: 1, pages: 3, ids: [1, 2, 3, 4, 5]);
    expect(find.text('1–5 of 12 articles'), findsOneWidget);
    expect(_pageButton(tester, previous: true).onPressed, isNull);
    expect(_pageButton(tester).onPressed, isNotNull);
    expect(imagePaths, {for (var id = 1; id <= 5; id++) _imagePath(id)});
    expect(
      tester
          .widgetList<NewsThumbnail>(find.byType(NewsThumbnail))
          .map((thumbnail) => thumbnail.imagePath),
      [for (var id = 1; id <= 5; id++) _imagePath(id)],
    );

    await _turnPage(tester);
    _expectPage(tester, page: 2, pages: 3, ids: [6, 7, 8, 9, 10]);
    expect(find.text('6–10 of 12 articles'), findsOneWidget);
    expect(_pageButton(tester, previous: true).onPressed, isNotNull);
    expect(_pageButton(tester).onPressed, isNotNull);

    await _turnPage(tester);
    _expectPage(tester, page: 3, pages: 3, ids: [11, 12]);
    expect(find.text('11–12 of 12 articles'), findsOneWidget);
    expect(_pageButton(tester).onPressed, isNull);
    expect(imagePaths, {for (var id = 1; id <= 12; id++) _imagePath(id)});

    await _turnPage(tester, previous: true);
    _expectPage(tester, page: 2, pages: 3, ids: [6, 7, 8, 9, 10]);
    await _turnPage(tester, previous: true);
    _expectPage(tester, page: 1, pages: 3, ids: [1, 2, 3, 4, 5]);
    expect(_pageButton(tester, previous: true).onPressed, isNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets('opens the selected article and image, then retains its page', (
    tester,
  ) async {
    await _pumpNews(tester, loadNews: () => _newsItems(12));
    await _turnPage(tester);

    await tester.tap(find.byKey(const ValueKey('news-article-7')));
    await tester.pumpAndSettle();

    expect(find.byType(NewsDetailScreen), findsOneWidget);
    expect(find.text('News Detail'), findsOneWidget);
    expect(find.text('Article 7'), findsOneWidget);
    expect(
      tester.widget<NewsDetailScreen>(find.byType(NewsDetailScreen)).item.id,
      'article-7',
    );
    expect(
      tester
          .widget<NewsThumbnail>(
            find.byKey(const ValueKey('news-detail-image')),
          )
          .imagePath,
      _imagePath(7),
    );
    expect(find.textContaining('Full content for article 7'), findsOneWidget);

    await tester.pageBack();
    await tester.pumpAndSettle();

    expect(find.byType(NewsDetailScreen), findsNothing);
    _expectPage(tester, page: 2, pages: 3, ids: [6, 7, 8, 9, 10]);
    expect(tester.takeException(), isNull);
  });

  testWidgets('refreshing a smaller dataset resets to a valid first page', (
    tester,
  ) async {
    var items = _newsItems(12);
    var loads = 0;
    await _pumpNews(
      tester,
      loadNews: () {
        loads++;
        return items;
      },
    );
    await _turnPage(tester);
    await _turnPage(tester);
    _expectPage(tester, page: 3, pages: 3, ids: [11, 12]);

    items = _newsItems(2);
    await tester
        .widget<RefreshIndicator>(find.byType(RefreshIndicator))
        .onRefresh();
    await tester.pumpAndSettle();

    expect(loads, 2);
    _expectPage(tester, page: 1, pages: 1, ids: [1, 2]);
    expect(find.text('1–2 of 2 articles'), findsOneWidget);
    expect(_pageButton(tester, previous: true).onPressed, isNull);
    expect(_pageButton(tester).onPressed, isNull);
    expect(tester.takeException(), isNull);
  });

  testWidgets('empty results show the empty message without pagination', (
    tester,
  ) async {
    final imagePaths = <String>{};
    await _pumpNews(tester, loadNews: () => const [], imagePaths: imagePaths);

    expect(find.text('No news is available yet.'), findsOneWidget);
    expect(find.byType(NewsCard), findsNothing);
    expect(find.byKey(const ValueKey('news-next-page')), findsNothing);
    expect(find.byKey(const ValueKey('news-previous-page')), findsNothing);
    expect(find.text('Try again'), findsNothing);
    expect(imagePaths, isEmpty);
  });

  testWidgets('failed loading shows retry and retry recovers the feed', (
    tester,
  ) async {
    var attempts = 0;
    await _pumpNews(
      tester,
      loadNews: () {
        attempts++;
        if (attempts == 1) throw StateError('Test news request failed');
        return _newsItems(2);
      },
    );

    expect(find.text('Unable to load news. Please try again.'), findsOneWidget);
    expect(find.text('No news is available yet.'), findsNothing);
    expect(find.byType(NewsCard), findsNothing);
    expect(find.byKey(const ValueKey('news-next-page')), findsNothing);

    await tester.tap(find.text('Try again'));
    await tester.pumpAndSettle();

    expect(attempts, 2);
    expect(find.text('Try again'), findsNothing);
    _expectPage(tester, page: 1, pages: 1, ids: [1, 2]);
    expect(tester.takeException(), isNull);
  });

  testWidgets('pending news shows loading before the completed feed', (
    tester,
  ) async {
    final pending = Completer<List<NewsItem>>();
    await _pumpNews(tester, loadNews: () => pending.future, settle: false);

    expect(find.byType(CircularProgressIndicator), findsOneWidget);
    expect(find.byType(NewsCard), findsNothing);

    pending.complete(_newsItems(1));
    await tester.pumpAndSettle();

    _expectPage(tester, page: 1, pages: 1, ids: [1]);
    expect(find.byType(CircularProgressIndicator), findsNothing);
  });
}

Future<void> _pumpNews(
  WidgetTester tester, {
  required FutureOr<List<NewsItem>> Function() loadNews,
  Set<String>? imagePaths,
  bool settle = true,
}) async {
  tester.view.physicalSize = const Size(430, 1400);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.resetPhysicalSize);
  addTearDown(tester.view.resetDevicePixelRatio);

  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        newsItemsProvider.overrideWith((ref) async => loadNews()),
        newsThumbnailBytesProvider.overrideWith((ref, path) async {
          imagePaths?.add(path);
          return _imageBytes;
        }),
      ],
      child: const MaterialApp(home: NewsScreen()),
    ),
  );
  if (settle) await tester.pumpAndSettle();
}

void _expectPage(
  WidgetTester tester, {
  required int page,
  required int pages,
  required List<int> ids,
}) {
  expect(find.text('Page $page of $pages'), findsOneWidget);
  expect(find.byType(NewsCard), findsNWidgets(ids.length));
  expect(
    tester
        .widgetList<NewsCard>(find.byType(NewsCard))
        .map((card) => card.item.id),
    ids.map((id) => 'article-$id'),
  );
}

IconButton _pageButton(WidgetTester tester, {bool previous = false}) =>
    tester.widget<IconButton>(
      find.byKey(ValueKey(previous ? 'news-previous-page' : 'news-next-page')),
    );

Future<void> _turnPage(WidgetTester tester, {bool previous = false}) async {
  final button = find.byKey(
    ValueKey(previous ? 'news-previous-page' : 'news-next-page'),
  );
  await tester.ensureVisible(button);
  await tester.tap(button);
  await tester.pumpAndSettle();
}

List<NewsItem> _newsItems(int count) => List.generate(count, (index) {
  final id = index + 1;
  return NewsItem(
    id: 'article-$id',
    title: 'Article $id',
    shortContent: 'Summary for article $id',
    content: '<p>Full content for article $id</p>',
    imageThumbnail: _imagePath(id),
    startDate: DateTime(2026, 9, 14).subtract(Duration(days: index)),
    dateCreated: DateTime(2026, 9, 14),
    createdBy: 'system',
  );
});

String _imagePath(int id) => '/uploads/image/article-$id.png';

final Uint8List _imageBytes = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+afo0AAAAASUVORK5CYII=',
);
