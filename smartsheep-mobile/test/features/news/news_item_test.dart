import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/features/news/news_item.dart';

void main() {
  group('NewsItem.fromJson', () {
    test('parses the PascalCase contract returned by the News API', () {
      final item = NewsItem.fromJson({
        'Id': 'news-15',
        'Title': 'Kontes Domba Batur',
        'ShortContent': 'Ringkasan berita dari API.',
        'Content': '<p>Isi lengkap berita.</p>',
        'ImageThumbnail': '/uploads/image/news-detik-015.jpg',
        'StartDate': '2026-08-30T19:30:23',
        'DateCreated': '2026-09-03T12:09:40.486422',
        'CreatedBy': 'system',
      });

      expect(item.id, 'news-15');
      expect(item.title, 'Kontes Domba Batur');
      expect(item.shortContent, 'Ringkasan berita dari API.');
      expect(item.content, '<p>Isi lengkap berita.</p>');
      expect(item.imageThumbnail, '/uploads/image/news-detik-015.jpg');
      expect(item.startDate, DateTime.parse('2026-08-30T19:30:23'));
      expect(item.dateCreated, DateTime.parse('2026-09-03T12:09:40.486422'));
      expect(item.createdBy, 'system');
      expect(item.latestDate, item.startDate);
    });

    test('accepts camelCase and safely handles optional malformed values', () {
      final item = NewsItem.fromJson({
        'id': ' news-1 ',
        'title': ' Berita Domba ',
        'shortContent': ' Ringkasan ',
        'content': ' Isi ',
        'imageThumbnail': '   ',
        'startDate': 'not-a-date',
        'dateCreated': DateTime(2026, 9, 4, 10, 30),
        'createdBy': ' admin ',
      });

      expect(item.id, 'news-1');
      expect(item.title, 'Berita Domba');
      expect(item.shortContent, 'Ringkasan');
      expect(item.content, 'Isi');
      expect(item.imageThumbnail, isNull);
      expect(item.startDate, isNull);
      expect(item.dateCreated, DateTime(2026, 9, 4, 10, 30));
      expect(item.createdBy, 'admin');
      expect(item.latestDate, item.dateCreated);
    });
  });

  group('latest News helpers', () {
    test('sorts newest first using StartDate with DateCreated as fallback', () {
      final original = <NewsItem>[
        _news('without-date'),
        _news('published-august', startDate: DateTime(2026, 8, 30)),
        _news('uploaded-september', dateCreated: DateTime(2026, 9, 1)),
        _news(
          'published-september',
          startDate: DateTime(2026, 9, 3),
          dateCreated: DateTime(2026, 7, 1),
        ),
      ];

      final sorted = sortNewsByLatest(original);

      expect(sorted.map((item) => item.id), [
        'published-september',
        'uploaded-september',
        'published-august',
        'without-date',
      ]);
      expect(original.map((item) => item.id), [
        'without-date',
        'published-august',
        'uploaded-september',
        'published-september',
      ]);
    });

    test('returns exactly the five newest items for the Home slider', () {
      final items = List.generate(
        7,
        (index) =>
            _news('news-${index + 1}', startDate: DateTime(2026, 9, index + 1)),
      );

      final result = latestNews(items.reversed);

      expect(latestNewsLimit, 5);
      expect(result, hasLength(5));
      expect(result.map((item) => item.id), [
        'news-7',
        'news-6',
        'news-5',
        'news-4',
        'news-3',
      ]);
    });

    test('honors a custom limit and treats a non-positive limit as empty', () {
      final items = [
        _news('older', startDate: DateTime(2026, 8, 1)),
        _news('newer', startDate: DateTime(2026, 9, 1)),
      ];

      expect(latestNews(items, limit: 1).single.id, 'newer');
      expect(latestNews(items, limit: 0), isEmpty);
      expect(latestNews(items, limit: -1), isEmpty);
    });
  });

  group('News pagination', () {
    final items = List.generate(12, (index) => _news('news-${index + 1}'));

    test('splits twelve items into pages of five, five, and two', () {
      expect(newsPageSize, 5);
      expect(newsPageCount(items.length), 3);
      expect(paginateNews(items, pageNumber: 1).map((item) => item.id), [
        'news-1',
        'news-2',
        'news-3',
        'news-4',
        'news-5',
      ]);
      expect(paginateNews(items, pageNumber: 2).map((item) => item.id), [
        'news-6',
        'news-7',
        'news-8',
        'news-9',
        'news-10',
      ]);
      expect(paginateNews(items, pageNumber: 3).map((item) => item.id), [
        'news-11',
        'news-12',
      ]);
    });

    test('returns empty results outside the valid one-based page range', () {
      expect(paginateNews(items, pageNumber: 0), isEmpty);
      expect(paginateNews(items, pageNumber: -1), isEmpty);
      expect(paginateNews(items, pageNumber: 4), isEmpty);
      expect(paginateNews(items, pageNumber: 1, pageSize: 0), isEmpty);
    });

    test('calculates page boundaries without an empty trailing page', () {
      expect(newsPageCount(0), 0);
      expect(newsPageCount(1), 1);
      expect(newsPageCount(5), 1);
      expect(newsPageCount(6), 2);
      expect(newsPageCount(10), 2);
      expect(newsPageCount(11), 3);
      expect(newsPageCount(10, pageSize: 0), 0);
    });
  });
}

NewsItem _news(String id, {DateTime? startDate, DateTime? dateCreated}) =>
    NewsItem(
      id: id,
      title: 'Title $id',
      shortContent: 'Summary $id',
      content: 'Content $id',
      imageThumbnail: '/uploads/image/$id.jpg',
      startDate: startDate,
      dateCreated: dateCreated,
      createdBy: 'system',
    );
