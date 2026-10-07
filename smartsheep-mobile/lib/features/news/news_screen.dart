import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../shell/member_header.dart';
import 'news_data.dart';
import 'news_feed.dart';

class NewsScreen extends ConsumerWidget {
  const NewsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) => Scaffold(
    body: Column(
      children: [
        const MemberHeader(
          title: 'News',
          subtitle: 'Latest updates & stories',
          showBackButton: true,
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () => ref.refresh(newsItemsProvider.future),
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.all(16),
              children: const [NewsFeed()],
            ),
          ),
        ),
      ],
    ),
  );
}
