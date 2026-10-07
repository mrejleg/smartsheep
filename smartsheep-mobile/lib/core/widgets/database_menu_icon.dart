import 'package:flutter/material.dart';
import 'package:flutter_svg/flutter_svg.dart';
import 'package:xml/xml.dart';

/// AppMenu.Icon and IconActive store SVG markup, just as the web renders it.
/// Missing icons use one neutral marker, never a guessed controller icon.
class DatabaseMenuIcon extends StatelessWidget {
  const DatabaseMenuIcon({
    super.key,
    this.icon,
    this.activeIcon,
    this.active = false,
    this.color,
    this.size = 24,
  });

  final String? icon;
  final String? activeIcon;
  final bool active;
  final Color? color;
  final double size;

  String? get resolvedIcon =>
      (active ? _inlineSvg(activeIcon) : null) ?? _inlineSvg(icon);

  @override
  Widget build(BuildContext context) {
    final svg = resolvedIcon;
    final foreground = color ?? IconTheme.of(context).color ?? Colors.black;
    Widget fallback() => Icon(
      Icons.circle_outlined,
      key: const ValueKey('database-menu-icon-fallback'),
      size: size,
      color: foreground,
    );
    return SizedBox(
      width: size,
      height: size,
      child: svg == null
          ? fallback()
          : SvgPicture.string(
              svg,
              key: const ValueKey('database-menu-icon-svg'),
              width: size,
              height: size,
              theme: SvgTheme(currentColor: foreground),
              fit: BoxFit.contain,
              excludeFromSemantics: true,
              placeholderBuilder: (_) => SizedBox(width: size, height: size),
              errorBuilder: (_, _, _) => fallback(),
            ),
    );
  }
}

String? _inlineSvg(String? value) {
  final svg = value?.trim();
  if (svg == null || svg.isEmpty || svg.length > 65536) return null;
  try {
    final document = XmlDocument.parse(svg);
    if (document.rootElement.name.local != 'svg' ||
        document.children.any((node) => node is XmlDoctype)) {
      return null;
    }
    // Database icons are local vector shapes. Do not load remote images,
    // scripts, stylesheets, fonts, or other resources embedded in markup.
    const vectorElements = {
      'svg',
      'g',
      'path',
      'polyline',
      'polygon',
      'line',
      'rect',
      'circle',
      'ellipse',
      'defs',
      'linearGradient',
      'radialGradient',
      'stop',
      'clipPath',
      'mask',
      'title',
      'desc',
      'use',
    };
    for (final element in document.descendants.whereType<XmlElement>()) {
      if (!vectorElements.contains(element.name.local)) return null;
      for (final attribute in element.attributes) {
        final name = attribute.name.local.toLowerCase();
        if (name.startsWith('on')) return null;
        if (name == 'href' && !attribute.value.trim().startsWith('#')) {
          return null;
        }
        final withoutLocalReferences = attribute.value.replaceAll(
          RegExp(r'url\(\s*#[a-zA-Z_][\w:.-]*\s*\)', caseSensitive: false),
          '',
        );
        if (RegExp(
          r'url\s*\(',
          caseSensitive: false,
        ).hasMatch(withoutLocalReferences)) {
          return null;
        }
      }
    }
    return svg;
  } on XmlException {
    return null;
  } on StateError {
    return null;
  }
}
