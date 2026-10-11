using InferenceYolo.Interfaces.Inferences;
using InferenceYolo.Models;
using InferenceYolo.Models.Inferences;
using InferenceYolo.Services.Inferences;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InferenceYolo.Dis
{
    public static class InferenceDi
    {
        // Satu scope = satu SourceVideo (mode manual: satu scope; worker: satu scope per kamera), sehingga
        // service yang menyimpan state (sumber video, detector, tracker, analyzer, pending, klien API) scoped.
        // InferenceOption scoped diisi pemanggil tepat setelah scope dibuat.
        public static IServiceCollection AddInferenceServices(this IServiceCollection services, AppSetting setting)
        {
            services.AddSingleton(setting);
            services.AddSingleton<ISucklingStatisticService, SucklingStatisticService>();
            services.AddScoped<InferenceOption>();
            services.AddScoped<IInferenceRunnerService, InferenceRunnerService>();
            services.AddScoped<IVideoSourceService, VideoSourceService>();
            services.AddScoped<IYoloDetectorService, YoloDetectorService>();
            services.AddScoped<ICentroidTrackerService, CentroidTrackerService>();
            services.AddScoped<ISucklingAnalyzerService, SucklingAnalyzerService>();
            services.AddScoped<ISucklingSyncService, SucklingSyncService>();
            services.AddScoped<IPendingEventService, PendingEventService>();
            services.AddScoped<IFrameAnnotatorService, FrameAnnotatorService>();
            return services;
        }

        // Koneksi API (Yolo:ApiUrl, Yolo:ClientId, Yolo:ClientSecret) dari 04.Api/appsettings.json, setting Yolo lainnya dari
        // SystemConfig di API (dipakai worker dan preview inferensi di WebApp)
        public static AppSetting ReadAppSetting(string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"appsettings tidak ditemukan: {fullPath}");
            }

            var configuration = new ConfigurationBuilder().AddJsonFile(fullPath, optional: false).Build();
            var setting = new AppSetting
            {
                YoloApiUrl = configuration["Yolo:ApiUrl"],
                YoloClientId = configuration["Yolo:ClientId"],
                YoloClientSecret = configuration["Yolo:ClientSecret"]
            };
            if (string.IsNullOrWhiteSpace(setting.YoloApiUrl) || string.IsNullOrWhiteSpace(setting.YoloClientId) || string.IsNullOrWhiteSpace(setting.YoloClientSecret))
            {
                throw new InvalidDataException($"Yolo:ApiUrl, Yolo:ClientId, dan Yolo:ClientSecret di {fullPath} wajib diisi");
            }

            using (var syncService = new SucklingSyncService(new InferenceOption(), setting))
            {
                syncService.ReadSystemConfig(setting);
            }

            // YoloSourceCodes boleh kosong (semua sumber video online diproses)
            var missing = typeof(AppSetting).GetProperties()
                .Where(p => p.Name != nameof(AppSetting.YoloSourceCodes) && string.IsNullOrWhiteSpace((string)p.GetValue(setting)))
                .Select(p => p.Name)
                .ToList();
            if (missing.Count > 0)
            {
                throw new InvalidDataException($"SystemConfig belum lengkap: {string.Join(", ", missing)}");
            }

            return setting;
        }
    }
}
