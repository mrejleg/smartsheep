import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../shell/member_header.dart';

class SheepBreedingScreen extends StatelessWidget {
  const SheepBreedingScreen({super.key});

  @override
  Widget build(BuildContext context) => Scaffold(
    body: Column(
      children: [
        const MemberHeader(
          title: 'Sheep Breeding',
          subtitle: 'Livestocks',
          showBackButton: true,
        ),
        Expanded(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const CircleAvatar(
                    radius: 36,
                    backgroundColor: AppColors.mint,
                    child: Icon(
                      Icons.favorite_outline,
                      size: 32,
                      color: AppColors.deepGreen,
                    ),
                  ),
                  const SizedBox(height: 18),
                  Text(
                    'Sheep Breeding',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Breeding management has not been configured yet.',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: AppColors.muted),
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
