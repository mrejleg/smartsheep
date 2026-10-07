import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import '../account/account_screen.dart';
import '../dashboard/dashboard_screen.dart';
import '../home/home_screen.dart';
import '../sources/sources_screen.dart';

class AppShell extends StatefulWidget {
  const AppShell({super.key});

  @override
  State<AppShell> createState() => _AppShellState();
}

class _AppShellState extends State<AppShell> {
  int _index = 0;
  final _visited = <int>{0};
  late final List<Widget> _pages;

  // Fixed route IDs are also used by Home shortcuts.
  static const _labels = ['Home', 'Dashboard', 'Sources', 'Account'];
  static const _keys = [
    'shell-tab-home',
    'shell-tab-dashboard',
    'shell-tab-sources',
    'shell-tab-account',
  ];
  static const _icons = [
    Icons.home_outlined,
    Icons.pie_chart_outline,
    Icons.videocam_outlined,
    Icons.person_outline,
  ];
  static const _selectedIcons = [
    Icons.home,
    Icons.pie_chart,
    Icons.videocam,
    Icons.person,
  ];

  @override
  void initState() {
    super.initState();
    _pages = [
      HomeScreen(onSelectTab: _selectTab),
      const DashboardScreen(),
      const SourcesScreen(),
      const AccountScreen(),
    ];
  }

  void _selectTab(int index) {
    if (index < 0 || index >= _pages.length || index == _index) return;
    setState(() {
      _index = index;
      _visited.add(index);
    });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: IndexedStack(
      index: _index,
      children: [
        for (var index = 0; index < _pages.length; index++)
          _visited.contains(index) ? _pages[index] : const SizedBox.shrink(),
      ],
    ),
    bottomNavigationBar: NavigationBar(
      selectedIndex: _index,
      height: 72,
      indicatorColor: AppColors.deepGreen,
      labelBehavior: NavigationDestinationLabelBehavior.alwaysShow,
      onDestinationSelected: _selectTab,
      destinations: [
        for (var index = 0; index < _pages.length; index++)
          NavigationDestination(
            key: ValueKey(_keys[index]),
            icon: Icon(_icons[index], color: AppColors.ink),
            selectedIcon: Icon(_selectedIcons[index], color: Colors.white),
            label: _labels[index],
          ),
      ],
    ),
  );
}
