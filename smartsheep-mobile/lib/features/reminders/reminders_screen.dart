import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/network/api_client.dart';
import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';

enum ReminderView { templates, logs, report }

const reminderPageSize = 20;

final reminderItemsProvider = FutureProvider.autoDispose
    .family<List<Map<String, dynamic>>, ({ReminderView view, int page})>((
      ref,
      request,
    ) async {
      final path = switch (request.view) {
        ReminderView.templates => '/api/ReminderTemplate/All',
        ReminderView.logs => '/api/ReminderLog/All',
        ReminderView.report => '/api/SmartSheepReport/Reminders',
      };
      final raw = await ref
          .watch(apiClientProvider)
          .get(
            path,
            query: request.view == ReminderView.report
                ? {'page': request.page, 'pageSize': reminderPageSize}
                : null,
          );
      if (raw is! List) throw StateError('Invalid reminder response.');
      final rows = raw
          .whereType<Map>()
          .map((row) => Map<String, dynamic>.from(row))
          .where((row) => _field(row, 'IsActive') != false)
          .toList();
      if (request.view != ReminderView.report) {
        rows.sort(
          (a, b) => request.view == ReminderView.templates
              ? _text(a, 'Name').compareTo(_text(b, 'Name'))
              : _text(b, 'ScheduledAt').compareTo(_text(a, 'ScheduledAt')),
        );
      }
      return rows;
    });

Object? _field(Map row, String key) =>
    row[key] ?? row['${key[0].toLowerCase()}${key.substring(1)}'];
String _text(Map row, String key) => '${_field(row, key) ?? ''}';
String _date(Object? value) {
  final date = DateTime.tryParse('$value');
  return date == null
      ? '—'
      : DateFormat('dd MMM yyyy HH:mm', 'en_US').format(date.toLocal());
}

class RemindersScreen extends ConsumerStatefulWidget {
  const RemindersScreen({super.key, required this.view});
  final ReminderView view;
  @override
  ConsumerState<RemindersScreen> createState() => _RemindersScreenState();
}

class _RemindersScreenState extends ConsumerState<RemindersScreen> {
  int _page = 1;
  final _scroll = ScrollController();
  @override
  void dispose() {
    _scroll.dispose();
    super.dispose();
  }

  Future<void> _refresh() async {
    setState(() => _page = 1);
    try {
      final provider = reminderItemsProvider((view: widget.view, page: 1));
      ref.invalidate(provider);
      await ref.read(provider.future);
    } catch (_) {
      // Display the API error instead of fabricated reminder samples.
    }
  }

  @override
  Widget build(BuildContext context) {
    final report = widget.view == ReminderView.report;
    final value = ref.watch(
      reminderItemsProvider((view: widget.view, page: report ? _page : 1)),
    );
    final title = switch (widget.view) {
      ReminderView.templates => 'Reminder Templates',
      ReminderView.logs => 'Reminder Logs',
      ReminderView.report => 'Reminder Report',
    };
    return Scaffold(
      body: Column(
        children: [
          MemberHeader(
            title: title,
            subtitle: widget.view == ReminderView.templates
                ? 'Configured reminder messages'
                : 'Reminder delivery history',
            showBackButton: true,
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: _refresh,
              child: value.when(
                loading: () => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: const [
                    SizedBox(height: 80),
                    Center(child: CircularProgressIndicator()),
                  ],
                ),
                error: (_, _) => ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  padding: const EdgeInsets.all(24),
                  children: [
                    const Text(
                      'Unable to load reminders. Please check your connection or access.',
                    ),
                    TextButton(onPressed: _refresh, child: const Text('Retry')),
                  ],
                ),
                data: (rows) {
                  final maxPage =
                      ((rows.length + reminderPageSize - 1) ~/ reminderPageSize)
                          .clamp(1, 1000000);
                  final page = report ? _page : _page.clamp(1, maxPage);
                  final visible = report
                      ? rows.take(reminderPageSize).toList()
                      : rows
                            .skip((page - 1) * reminderPageSize)
                            .take(reminderPageSize)
                            .toList();
                  final hasNext = report
                      ? rows.length > reminderPageSize
                      : page < maxPage;
                  return ListView(
                    controller: _scroll,
                    key: const ValueKey('reminders-list'),
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                    children: [
                      if (visible.isEmpty)
                        const Padding(
                          padding: EdgeInsets.all(24),
                          child: Text(
                            'No reminder data available.',
                            textAlign: TextAlign.center,
                          ),
                        ),
                      for (final row in visible)
                        _ReminderCard(
                          row: row,
                          template: widget.view == ReminderView.templates,
                        ),
                      if (page > 1 || hasNext)
                        Row(
                          children: [
                            IconButton(
                              tooltip: 'Previous page',
                              onPressed: page > 1
                                  ? () => _changePage(page - 1)
                                  : null,
                              icon: const Icon(Icons.chevron_left),
                            ),
                            Expanded(
                              child: Text(
                                'Page $page',
                                textAlign: TextAlign.center,
                              ),
                            ),
                            IconButton(
                              tooltip: 'Next page',
                              onPressed: hasNext
                                  ? () => _changePage(page + 1)
                                  : null,
                              icon: const Icon(Icons.chevron_right),
                            ),
                          ],
                        ),
                    ],
                  );
                },
              ),
            ),
          ),
        ],
      ),
    );
  }

  void _changePage(int page) {
    setState(() => _page = page);
    if (_scroll.hasClients) _scroll.jumpTo(0);
  }
}

class _ReminderCard extends StatelessWidget {
  const _ReminderCard({required this.row, required this.template});
  final Map<String, dynamic> row;
  final bool template;
  @override
  Widget build(BuildContext context) {
    final related = _field(row, 'ReminderTemplate');
    final message = template
        ? row
        : related is Map
        ? related
        : const {};
    final name = _text(message, 'Name');
    final error = _text(row, 'ErrorMessage');
    return Card(
      key: ValueKey('reminder-${_field(row, 'Id')}'),
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              name.isEmpty ? 'Reminder' : name,
              style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 16),
            ),
            if (_text(message, 'Code').isNotEmpty)
              Text(
                _text(message, 'Code'),
                style: const TextStyle(color: AppColors.muted, fontSize: 12),
              ),
            const SizedBox(height: 8),
            Text(
              _text(message, 'ReminderText').isEmpty
                  ? 'Reminder template content unavailable.'
                  : _text(message, 'ReminderText'),
            ),
            if (!template) ...[
              const Divider(height: 24),
              Text(
                'Status: ${_text(row, 'Status')}',
                style: const TextStyle(fontWeight: FontWeight.w600),
              ),
              Text('Username: ${_text(row, 'Username')}'),
              Text('Scheduled: ${_date(_field(row, 'ScheduledAt'))}'),
              Text('Sent: ${_date(_field(row, 'SentAt'))}'),
              if (error.isNotEmpty)
                Text(error, style: const TextStyle(color: AppColors.danger)),
            ],
          ],
        ),
      ),
    );
  }
}
