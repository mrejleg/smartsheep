#!/bin/bash
set -e

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$PROJECT_DIR"

echo "🐑 SmartSheep Mobile"
echo ""

# Check Flutter
flutter --version > /dev/null 2>&1 || { echo "❌ Flutter not found"; exit 1; }

# Get dependencies
echo "📦 Getting dependencies..."
flutter pub get > /dev/null 2>&1
echo "✓ Ready"
echo ""

# Show config
echo "⚙️  Config (assets/config.json):"
cat assets/config.json | jq '.' 2>/dev/null || cat assets/config.json
echo ""

# Show devices
echo "📱 Devices:"
flutter devices | grep -E "^\s*[a-z]|^Found"
echo ""

# Run
echo "🚀 Running..."
flutter run
