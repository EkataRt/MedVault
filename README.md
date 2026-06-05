# MedVault App

A medical vault application built with Ionic + Angular + Capacitor.

## Prerequisites

- Angular
- Node.js
- npm
- Ionic CLI (`npm install -g @ionic/cli`)

## Getting Started

### Install dependencies

npm install

### Run the app (web)

npm run dev

This starts both the mock server (port 3000) and the Ionic app (port 8100).

### Run on Android (USB)

1. Connect your phone via USB with USB Debugging enabled
2. Run: adb reverse tcp:3000 tcp:3000
3. Run: npm run build && npx cap sync android && npx cap run android --target YOUR_DEVICE_ID

## Project Structure

- `src/` - Angular/Ionic source code
- `server/` - Mock JSON server
- `android/` - Android native project
