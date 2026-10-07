import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';

class NewsSourceField {
  const NewsSourceField({required this.label, required this.value});

  final String label;
  final String value;
}

/// Keeps source labels and wrapped values in separate, consistently aligned
/// columns. Small screens and large accessibility text use a stacked layout.
class NewsSourceInfo extends StatelessWidget {
  const NewsSourceInfo({super.key, required this.fields});

  final List<NewsSourceField> fields;

  @override
  Widget build(BuildContext context) => Container(
    key: const ValueKey('news-source-info'),
    padding: const EdgeInsets.all(16),
    decoration: BoxDecoration(
      color: AppColors.surface,
      borderRadius: BorderRadius.circular(12),
      border: Border.all(color: const Color(0xFFE5EBF0)),
    ),
    child: LayoutBuilder(
      builder: (context, constraints) {
        final stacked =
            constraints.maxWidth < 300 ||
            MediaQuery.textScalerOf(context).scale(14) > 18;
        const labelStyle = TextStyle(
          fontSize: 13,
          height: 1.5,
          fontWeight: FontWeight.w600,
          color: AppColors.muted,
        );
        const valueStyle = TextStyle(
          fontSize: 14,
          height: 1.5,
          color: AppColors.ink,
        );

        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Semantics(
              header: true,
              child: const Text(
                'Informasi berita',
                style: TextStyle(
                  fontSize: 15,
                  height: 1.4,
                  fontWeight: FontWeight.w700,
                  color: AppColors.deepGreen,
                ),
              ),
            ),
            const SizedBox(height: 16),
            for (var index = 0; index < fields.length; index++) ...[
              if (index > 0) const SizedBox(height: 12),
              Semantics(
                container: true,
                child: stacked
                    ? Column(
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          Text(fields[index].label, style: labelStyle),
                          const SizedBox(height: 4),
                          Text(fields[index].value, style: valueStyle),
                        ],
                      )
                    : Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          SizedBox(
                            width: 104,
                            child: Text(fields[index].label, style: labelStyle),
                          ),
                          const SizedBox(
                            width: 12,
                            child: Text(':', style: labelStyle),
                          ),
                          Expanded(
                            child: Text(fields[index].value, style: valueStyle),
                          ),
                        ],
                      ),
              ),
            ],
          ],
        );
      },
    ),
  );
}
