using System.Collections.Generic;
using Sentry;
using Sentry.Unity;
using UnityEngine;

/// <summary>
/// Sistema de logging unificado que integra Unity Debug.Log com Sentry
/// Uso: AppLogger.Info("Mensagem"), AppLogger.Error("Erro", exception)
/// </summary>
public static class AppLogger
{
    // Controla se envia logs para o Sentry (útil para dev vs produção)
    public static bool EnableSentryLogging = true;

    // Controla se mostra logs no console do Unity
    public static bool EnableUnityLogging = true;

    /// <summary>
    /// Log informativo - apenas console Unity, NÃO envia para Sentry
    /// Use para fluxo normal da aplicação
    /// </summary>
    public static void Info(string message, string context = null)
    {
        if (EnableUnityLogging)
        {
            Debug.Log(FormatMessage("INFO", message, context));
        }

        // INFO não vai para Sentry - evita poluir o dashboard
    }

    /// <summary>
    /// Log de aviso - console Unity + Sentry com severidade Warning
    /// Use para situações anormais mas recuperáveis
    /// </summary>
    public static void Warning(string message, string context = null, Dictionary<string, object> extras = null)
    {
        if (EnableUnityLogging)
        {
            Debug.LogWarning(FormatMessage("WARN", message, context));
        }

        if (EnableSentryLogging)
        {
            SentrySdk.CaptureMessage(message, scope =>
            {
                scope.Level = SentryLevel.Warning;

                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);

                AddExtras(scope, extras);
            });
        }
    }

    /// <summary>
    /// Log de erro - console Unity + Sentry com severidade Error
    /// Use para erros que não quebram a aplicação
    /// </summary>
    public static void Error(string message, string context = null, Dictionary<string, object> extras = null)
    {
        if (EnableUnityLogging)
        {
            Debug.LogError(FormatMessage("ERROR", message, context));
        }

        if (EnableSentryLogging)
        {
            SentrySdk.CaptureMessage(message, scope =>
            {
                scope.Level = SentryLevel.Error;

                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);

                AddExtras(scope, extras);
            });
        }
    }

    /// <summary>
    /// Captura exceção - console Unity + Sentry
    /// Use para exceptions capturadas em try-catch
    /// </summary>
    public static void Exception(System.Exception exception, string context = null, Dictionary<string, object> extras = null)
    {
        if (EnableUnityLogging)
        {
            Debug.LogError(FormatMessage("EXCEPTION", exception.Message, context));
            Debug.LogException(exception);
        }

        if (EnableSentryLogging)
        {
            SentrySdk.CaptureException(exception, scope =>
            {
                if (!string.IsNullOrEmpty(context))
                    scope.SetTag("context", context);

                AddExtras(scope, extras);
            });
        }
    }

    /// <summary>
    /// Adiciona breadcrumb - rastro de navegação para debug
    /// Use para rastrear ações do usuário
    /// </summary>
    public static void Breadcrumb(string message, string category = "default", Dictionary<string, string> data = null)
    {
        if (EnableSentryLogging)
        {
            SentrySdk.AddBreadcrumb(
                message: message,
                category: category,
                level: BreadcrumbLevel.Info,
                data: data
            );
        }
    }

    // Métodos auxiliares privados

    private static string FormatMessage(string level, string message, string context)
    {
        if (string.IsNullOrEmpty(context))
            return $"[{level}] {message}";

        return $"[{level}][{context}] {message}";
    }

    private static void AddExtras(Scope scope, Dictionary<string, object> extras)
    {
        if (extras == null) return;

        foreach (var extra in extras)
        {
            scope.SetExtra(extra.Key, extra.Value);
        }
    }
}