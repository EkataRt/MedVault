import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'io.ionic.starter',
  appName: 'MedVault-app',
  webDir: 'www',
  android: {
    allowMixedContent: true,
  },
  plugins: {
    Camera: {
      presentationStyle: 'fullscreen',
    },
  },
  server: {
    androidScheme: 'https',
    cleartext: true,
  },
};

export default config;
