import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:intl/intl.dart';
import 'package:latlong2/latlong.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/theme/app_theme.dart';
import 'dashboard_data.dart';

LatLng? dashboardFarmCoordinates(DashboardFarmMap farm) {
  final latitude = double.tryParse(farm.latitude ?? '');
  final longitude = double.tryParse(farm.longitude ?? '');
  if (latitude == null ||
      longitude == null ||
      !latitude.isFinite ||
      !longitude.isFinite ||
      latitude.abs() > 90 ||
      longitude.abs() > 180) {
    return null;
  }
  return LatLng(latitude, longitude);
}

bool dashboardFarmIsOnline(DashboardFarmMap farm) =>
    farm.totalSourceVideos > 0 &&
    farm.onlineSourceVideos == farm.totalSourceVideos;

class DashboardFarmMapCard extends StatefulWidget {
  const DashboardFarmMapCard({
    super.key,
    required this.items,
    this.tileProvider,
  });
  final List<DashboardFarmMap> items;
  final TileProvider? tileProvider;

  @override
  State<DashboardFarmMapCard> createState() => _DashboardFarmMapCardState();
}

class _DashboardFarmMapCardState extends State<DashboardFarmMapCard> {
  final _controller = MapController();
  late final TileProvider _tiles = widget.tileProvider ?? NetworkTileProvider();
  DashboardFarmMap? _selected;

  @override
  void didUpdateWidget(covariant DashboardFarmMapCard oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (!identical(widget.items, oldWidget.items)) _selected = null;
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _zoom(double delta) => _controller.move(
    _controller.camera.center,
    (_controller.camera.zoom + delta).clamp(2.0, 18.0),
  );

  @override
  Widget build(BuildContext context) {
    final farms =
        widget.items
            .where((farm) => dashboardFarmCoordinates(farm) != null)
            .toList()
          ..sort(
            (left, right) => (dashboardFarmIsOnline(left) ? 1 : 0).compareTo(
              dashboardFarmIsOnline(right) ? 1 : 0,
            ),
          );
    return Card(
      clipBehavior: Clip.antiAlias,
      child: SizedBox(
        height: 280,
        child: Stack(
          children: [
            Semantics(
              label: 'Farm locations',
              child: FlutterMap(
                mapController: _controller,
                options: MapOptions(
                  initialCenter: const LatLng(-2.5489, 117.0940),
                  initialZoom: 5,
                  initialCameraFit: CameraFit.bounds(
                    bounds: LatLngBounds(
                      const LatLng(-11.2, 94.5),
                      const LatLng(6.3, 141.5),
                    ),
                    padding: const EdgeInsets.all(18),
                    maxZoom: 5,
                  ),
                  // Same Indonesia bounds as Web, with room to fit phone width.
                  minZoom: 2,
                  maxZoom: 18,
                  backgroundColor: const Color(0xFFD7E5E8),
                  interactionOptions: const InteractionOptions(
                    flags: InteractiveFlag.all & ~InteractiveFlag.rotate,
                  ),
                  onTap: (_, _) => setState(() => _selected = null),
                ),
                children: [
                  TileLayer(
                    urlTemplate:
                        'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'com.pertamina.smartsheepMobile',
                    tileProvider: _tiles,
                    maxNativeZoom: 18,
                  ),
                  MarkerLayer(
                    markers: [
                      for (final farm in farms)
                        Marker(
                          point: dashboardFarmCoordinates(farm)!,
                          alignment: Alignment.topCenter,
                          width: 36,
                          height: 42,
                          child: Tooltip(
                            message: farm.name ?? farm.code ?? 'Farm',
                            child: GestureDetector(
                              key: ValueKey('dashboard-farm-${farm.id}'),
                              onTap: () => setState(() => _selected = farm),
                              child: Stack(
                                alignment: Alignment.center,
                                children: [
                                  const Icon(
                                    Icons.location_on,
                                    color: Colors.white,
                                    size: 37,
                                  ),
                                  Icon(
                                    Icons.location_on,
                                    size: dashboardFarmIsOnline(farm) ? 32 : 27,
                                    color: dashboardFarmIsOnline(farm)
                                        ? const Color(0xFF078B20)
                                        : const Color(0xFF2563EB),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
            Positioned(
              top: 10,
              left: 10,
              child: Material(
                elevation: 2,
                color: Colors.white,
                borderRadius: BorderRadius.circular(8),
                child: Column(
                  children: [
                    IconButton(
                      tooltip: 'Zoom in',
                      onPressed: () => _zoom(1),
                      icon: const Icon(Icons.add),
                      visualDensity: VisualDensity.compact,
                    ),
                    const SizedBox(width: 32, child: Divider(height: 1)),
                    IconButton(
                      tooltip: 'Zoom out',
                      onPressed: () => _zoom(-1),
                      icon: const Icon(Icons.remove),
                      visualDensity: VisualDensity.compact,
                    ),
                  ],
                ),
              ),
            ),
            if (_selected != null)
              Positioned(
                left: 12,
                right: 12,
                bottom: 28,
                child: ConstrainedBox(
                  constraints: const BoxConstraints(maxHeight: 240),
                  child: SingleChildScrollView(
                    child: _FarmPopup(
                      farm: _selected!,
                      onClose: () => setState(() => _selected = null),
                    ),
                  ),
                ),
              ),
            Positioned(
              right: 0,
              bottom: 0,
              child: Material(
                color: Colors.white.withValues(alpha: .9),
                child: InkWell(
                  onTap: () => launchUrl(
                    Uri.parse('https://www.openstreetmap.org/copyright'),
                    mode: LaunchMode.externalApplication,
                  ),
                  child: const Padding(
                    padding: EdgeInsets.symmetric(horizontal: 6, vertical: 4),
                    child: Text(
                      '© OpenStreetMap contributors',
                      style: TextStyle(fontSize: 10, color: AppColors.tealDark),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _FarmPopup extends StatelessWidget {
  const _FarmPopup({required this.farm, required this.onClose});
  final DashboardFarmMap farm;
  final VoidCallback onClose;

  @override
  Widget build(BuildContext context) {
    final number = NumberFormat.decimalPattern('en_US');
    return Material(
      key: const ValueKey('dashboard-farm-popup'),
      elevation: 4,
      borderRadius: BorderRadius.circular(12),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                const Icon(Icons.home_outlined, color: AppColors.tealDark),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    farm.name ?? farm.code ?? '—',
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      fontSize: 13,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ),
                IconButton(
                  tooltip: 'Close farm details',
                  onPressed: onClose,
                  icon: const Icon(Icons.close, size: 18),
                  visualDensity: VisualDensity.compact,
                ),
              ],
            ),
            Text(
              farm.location ?? farm.address ?? '—',
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: AppColors.muted, fontSize: 11),
            ),
            const Divider(height: 18),
            Row(
              children: [
                _FarmMetric(
                  label: 'Barns',
                  value: number.format(farm.totalBarns),
                ),
                _FarmMetric(
                  label: 'Sheep',
                  value: number.format(farm.totalSheep),
                ),
                _FarmMetric(
                  label: 'Online',
                  value:
                      '${number.format(farm.onlineSourceVideos)}/${number.format(farm.totalSourceVideos)}',
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _FarmMetric extends StatelessWidget {
  const _FarmMetric({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) => Expanded(
    child: Column(
      children: [
        Text(
          value,
          style: const TextStyle(
            fontSize: 16,
            fontWeight: FontWeight.w800,
            color: AppColors.tealDark,
          ),
        ),
        const SizedBox(height: 3),
        Text(
          label,
          style: const TextStyle(fontSize: 11, color: AppColors.muted),
        ),
      ],
    ),
  );
}
