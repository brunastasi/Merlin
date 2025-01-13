namespace Moana.Services
{
    using System;
    using System.IO;
    using Microsoft.Extensions.Logging;

    public class LoggerService
    {
        private readonly Dictionary<string, string> _logFiles = new();

        public LoggerService()
        {
            // Initialisation des fichiers de logs
            _logFiles["APPLICATION"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs/APPLICATION-{DateTime.Now:yyyy-MM-dd}.log");
            _logFiles["API"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs/API-{DateTime.Now:yyyy-MM-dd}.log");
            _logFiles["TRADING"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs/TRADING-{DateTime.Now:yyyy-MM-dd}.log");
            _logFiles["DATA"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs/DATA-{DateTime.Now:yyyy-MM-dd}.log");
            _logFiles["ERROR"] = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Logs/ERROR-{DateTime.Now:yyyy-MM-dd}.log");
        }

        private void WriteLog(string category, string level, string message)
        {
            if (!_logFiles.ContainsKey(category))
            {
                category = "APPLICATION"; // Catégorie par défaut si non trouvée
            }

            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {level.ToUpper()} - {message}{Environment.NewLine}";
            string filePath = _logFiles[category];

            try
            {
                // Créer le répertoire si nécessaire
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Écrire le log uniquement si le fichier n'existe pas encore
                File.AppendAllText(filePath, logMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture dans le fichier de log {filePath}: {ex.Message}");
            }
        }

        public void LogInformation(string message, string category = "APPLICATION")
        {
            WriteLog(category, "INFO", message);
        }

        public void LogWarning(string message, string category = "APPLICATION")
        {
            WriteLog(category, "WARNING", message);
        }

        public void LogError(string message, string category = "APPLICATION")
        {
            WriteLog(category, "ERROR", message);
        }
    }


}
