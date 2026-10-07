import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';
import 'news_content.dart';
import 'news_item.dart';
import 'news_thumbnail.dart';

class NewsDetailScreen extends StatelessWidget {
  const NewsDetailScreen({super.key, required this.item});

  final NewsItem item;

  @override
  Widget build(BuildContext context) {
    final date = item.latestDate;
    final title = newsPlainText(item.title);
    final article = newsArticle(item.content);
    return Scaffold(
      backgroundColor: Colors.white,
      body: Column(
        children: [
          const MemberHeader(
            title: 'News Detail',
            subtitle: 'News & stories',
            showBackButton: true,
          ),
          Expanded(
            child: SafeArea(
              top: false,
              child: SingleChildScrollView(
                key: const PageStorageKey('news-detail-scroll'),
                physics: const AlwaysScrollableScrollPhysics(),
                child: Align(
                  alignment: Alignment.topCenter,
                  child: ConstrainedBox(
                    constraints: const BoxConstraints(maxWidth: 760),
                    child: Padding(
                      padding: const EdgeInsets.fromLTRB(20, 20, 20, 32),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          AspectRatio(
                            aspectRatio: 16 / 9,
                            child: NewsThumbnail(
                              key: const ValueKey('news-detail-image'),
                              imagePath: item.imageThumbnail,
                              width: double.infinity,
                              height: double.infinity,
                              borderRadius: BorderRadius.circular(16),
                              semanticLabel: title.isEmpty
                                  ? 'News image'
                                  : title,
                            ),
                          ),
                          const SizedBox(height: 22),
                          Wrap(
                            spacing: 12,
                            runSpacing: 8,
                            crossAxisAlignment: WrapCrossAlignment.center,
                            children: [
                              if (article.category != null)
                                Text(
                                  article.category!,
                                  key: const ValueKey('news-detail-category'),
                                  style: const TextStyle(
                                    color: AppColors.tealDark,
                                    fontSize: 12,
                                    fontWeight: FontWeight.w800,
                                    letterSpacing: 1.2,
                                  ),
                                ),
                              if (date != null)
                                Row(
                                  mainAxisSize: MainAxisSize.min,
                                  children: [
                                    const Icon(
                                      Icons.calendar_today_outlined,
                                      size: 13,
                                      color: AppColors.muted,
                                    ),
                                    const SizedBox(width: 5),
                                    Flexible(
                                      child: Text(
                                        DateFormat(
                                          'd MMM yyyy HH:mm',
                                        ).format(date.toLocal()),
                                        style: const TextStyle(
                                          color: AppColors.muted,
                                          fontSize: 12,
                                        ),
                                      ),
                                    ),
                                  ],
                                ),
                            ],
                          ),
                          const SizedBox(height: 12),
                          Semantics(
                            header: true,
                            child: Text(
                              title.isEmpty ? 'SmartSheep News' : title,
                              style: const TextStyle(
                                color: AppColors.ink,
                                fontSize: 26,
                                fontWeight: FontWeight.w800,
                                height: 1.3,
                              ),
                            ),
                          ),
                          const SizedBox(height: 20),
                          const Divider(height: 1),
                          const SizedBox(height: 20),
                          NewsContent(
                            content: article.content,
                            summary: item.shortContent,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
