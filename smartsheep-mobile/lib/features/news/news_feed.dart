import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import 'news_content.dart';
import 'news_data.dart';
import 'news_detail_screen.dart';
import 'news_item.dart';
import 'news_thumbnail.dart';

/// Home and News share this section; the enclosing page owns vertical scrolling.
class NewsFeed extends ConsumerWidget {
  const NewsFeed({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => ref
      .watch(newsItemsProvider)
      .when(
        skipLoadingOnRefresh: false,
        loading: () => const Padding(
          padding: EdgeInsets.all(32),
          child: Center(child: CircularProgressIndicator()),
        ),
        error: (_, _) => _NewsStatus(
          message: 'Unable to load news. Please try again.',
          onRetry: () => ref.invalidate(newsItemsProvider),
        ),
        data: (items) => items.isEmpty
            ? const _NewsStatus(message: 'No news is available yet.')
            : _PaginatedNewsList(items: items),
      );
}

class _PaginatedNewsList extends StatefulWidget {
  const _PaginatedNewsList({required this.items});
  final List<NewsItem> items;

  @override
  State<_PaginatedNewsList> createState() => _PaginatedNewsListState();
}

class _PaginatedNewsListState extends State<_PaginatedNewsList> {
  final _topKey = GlobalKey();
  int _page = 1;

  @override
  void didUpdateWidget(covariant _PaginatedNewsList oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!identical(oldWidget.items, widget.items)) _page = 1;
  }

  void _changePage(int page) {
    setState(() => _page = page);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final target = _topKey.currentContext;
      if (target != null) {
        Scrollable.ensureVisible(
          target,
          duration: const Duration(milliseconds: 250),
          curve: Curves.easeOutCubic,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final items = paginateNews(widget.items, pageNumber: _page);
    final pages = newsPageCount(widget.items.length);
    final first = (_page - 1) * newsPageSize + 1;
    final last = first + items.length - 1;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Padding(
          key: _topKey,
          padding: const EdgeInsets.only(bottom: 12),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  '$first–$last of ${widget.items.length} articles',
                  style: const TextStyle(color: AppColors.muted, fontSize: 12),
                ),
              ),
              IconButton(
                key: const ValueKey('news-previous-page-top'),
                tooltip: 'Previous news page',
                onPressed: _page > 1 ? () => _changePage(_page - 1) : null,
                icon: const Icon(Icons.chevron_left),
              ),
              IconButton(
                key: const ValueKey('news-next-page-top'),
                tooltip: 'Next news page',
                onPressed: _page < pages ? () => _changePage(_page + 1) : null,
                icon: const Icon(Icons.chevron_right),
              ),
            ],
          ),
        ),
        for (final item in items)
          Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: NewsCard(key: ValueKey('news-${item.id}'), item: item),
          ),
        Row(
          children: [
            IconButton.outlined(
              key: const ValueKey('news-previous-page'),
              tooltip: 'Previous news page',
              onPressed: _page > 1 ? () => _changePage(_page - 1) : null,
              icon: const Icon(Icons.chevron_left),
            ),
            Expanded(
              child: Semantics(
                liveRegion: true,
                child: Text(
                  'Page $_page of $pages',
                  textAlign: TextAlign.center,
                  style: const TextStyle(fontWeight: FontWeight.w600),
                ),
              ),
            ),
            IconButton.outlined(
              key: const ValueKey('news-next-page'),
              tooltip: 'Next news page',
              onPressed: _page < pages ? () => _changePage(_page + 1) : null,
              icon: const Icon(Icons.chevron_right),
            ),
          ],
        ),
      ],
    );
  }
}

class NewsCard extends StatelessWidget {
  const NewsCard({super.key, required this.item});
  final NewsItem item;

  @override
  Widget build(BuildContext context) {
    final plainTitle = newsPlainText(item.title);
    final title = plainTitle.isEmpty ? 'SmartSheep News' : plainTitle;
    final summary = newsPlainText(item.shortContent);
    final date = item.latestDate;

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => Navigator.of(context).push(
          MaterialPageRoute<void>(builder: (_) => NewsDetailScreen(item: item)),
        ),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              NewsThumbnail(
                imagePath: item.imageThumbnail,
                width: 88,
                height: 88,
                semanticLabel: title,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      maxLines: 3,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 14,
                        fontWeight: FontWeight.w700,
                        height: 1.35,
                      ),
                    ),
                    if (summary.isNotEmpty) ...[
                      const SizedBox(height: 6),
                      Text(
                        summary,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: const TextStyle(
                          color: AppColors.muted,
                          fontSize: 12,
                          height: 1.4,
                        ),
                      ),
                    ],
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            date == null
                                ? 'Read article'
                                : DateFormat(
                                    'd MMM yyyy HH:mm',
                                  ).format(date.toLocal()),
                            style: const TextStyle(
                              fontSize: 11,
                              color: AppColors.tealDark,
                            ),
                          ),
                        ),
                        const Icon(
                          Icons.arrow_forward,
                          size: 16,
                          color: AppColors.tealDark,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _NewsStatus extends StatelessWidget {
  const _NewsStatus({required this.message, this.onRetry});
  final String message;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 32, horizontal: 16),
    child: Column(
      children: [
        const Icon(Icons.newspaper_outlined, size: 44, color: AppColors.muted),
        const SizedBox(height: 12),
        Text(
          message,
          textAlign: TextAlign.center,
          style: const TextStyle(color: AppColors.muted),
        ),
        if (onRetry != null) ...[
          const SizedBox(height: 12),
          OutlinedButton(onPressed: onRetry, child: const Text('Try again')),
        ],
      ],
    ),
  );
}
