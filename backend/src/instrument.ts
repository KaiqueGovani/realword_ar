import * as Sentry from '@sentry/nestjs';

Sentry.init({
  dsn: 'https://dec6fae97c283a1c9fd13f2ae0b98edd@o4510280804007936.ingest.us.sentry.io/4510280806367232',

  // Send structured logs to Sentry
  enableLogs: true,
  integrations: [Sentry.consoleLoggingIntegration({ levels: ['log', 'warn', 'error'] })],
  // Tracing
  tracesSampleRate: 1.0, //  Capture 100% of the transactions
  // Setting this option to true will send default PII data to Sentry.
  // For example, automatic IP address collection on events
  sendDefaultPii: true,
});
