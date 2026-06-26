namespace AppGen.Core.Capabilities;

public static class MobileCapabilityCatalog
{
    private static readonly IReadOnlyList<MobileCapabilityDefinition> All =
    [
        // Connectivity
        Def(MobileCapabilityId.Internet, "Connectivity", "Internet",
            "HTTP client included via dio in every generated app", implemented: true),

        Def(MobileCapabilityId.Bluetooth, "Connectivity", "Bluetooth", implemented: true,
            packages: [Pkg("flutter_blue_plus", "^1.35.3"), Pkg("permission_handler", "^11.3.1")],
            android: ["BLUETOOTH_SCAN", "BLUETOOTH_CONNECT", "ACCESS_FINE_LOCATION"],
            ios: ["NSBluetoothAlwaysUsageDescription"],
            service: "bluetooth_service.dart.scriban", file: "bluetooth_service.dart"),

        Def(MobileCapabilityId.Nfc, "Connectivity", "NFC", implemented: true,
            packages: [Pkg("nfc_manager", "^3.5.0")],
            android: ["NFC"],
            service: "nfc_service.dart.scriban", file: "nfc_service.dart"),

        Def(MobileCapabilityId.Wifi, "Connectivity", "WiFi", implemented: true,
            packages: [Pkg("network_info_plus", "^6.0.1")],
            service: "wifi_service.dart.scriban", file: "wifi_service.dart"),

        // Location
        Def(MobileCapabilityId.Gps, "Location", "GPS", implemented: true,
            packages: [Pkg("geolocator", "^13.0.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["ACCESS_FINE_LOCATION", "ACCESS_COARSE_LOCATION"],
            ios: ["NSLocationWhenInUseUsageDescription"],
            service: "location_service.dart.scriban", file: "location_service.dart"),

        Def(MobileCapabilityId.Maps, "Location", "Maps", implemented: true,
            packages: [Pkg("google_maps_flutter", "^2.9.0")],
            android: ["ACCESS_FINE_LOCATION", "ACCESS_COARSE_LOCATION", "INTERNET"],
            ios: ["NSLocationWhenInUseUsageDescription"],
            service: "maps_service.dart.scriban", file: "maps_service.dart"),

        Def(MobileCapabilityId.Geofencing, "Location", "Geofencing", implemented: true,
            packages: [Pkg("geolocator", "^13.0.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["ACCESS_FINE_LOCATION", "ACCESS_BACKGROUND_LOCATION"],
            ios: ["NSLocationAlwaysAndWhenInUseUsageDescription"],
            service: "geofencing_service.dart.scriban", file: "geofencing_service.dart"),

        Def(MobileCapabilityId.PlaceSearch, "Location", "Place Search", implemented: true,
            packages: [Pkg("geocoding", "^3.0.0")],
            service: "place_search_service.dart.scriban", file: "place_search_service.dart"),

        // Media
        Def(MobileCapabilityId.Camera, "Media", "Camera", implemented: true,
            packages: [Pkg("image_picker", "^1.1.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["CAMERA"],
            ios: ["NSCameraUsageDescription"],
            service: "camera_service.dart.scriban", file: "camera_service.dart"),

        Def(MobileCapabilityId.Gallery, "Media", "Gallery", implemented: true,
            packages: [Pkg("image_picker", "^1.1.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["READ_MEDIA_IMAGES"],
            ios: ["NSPhotoLibraryUsageDescription"],
            service: "gallery_service.dart.scriban", file: "gallery_service.dart"),

        Def(MobileCapabilityId.VideoRecording, "Media", "Video Recording", implemented: true,
            packages: [Pkg("image_picker", "^1.1.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["CAMERA", "RECORD_AUDIO"],
            ios: ["NSCameraUsageDescription", "NSMicrophoneUsageDescription"],
            service: "video_recording_service.dart.scriban", file: "video_recording_service.dart"),

        Def(MobileCapabilityId.AudioRecording, "Media", "Audio Recording", implemented: true,
            packages: [Pkg("record", "^5.1.2"), Pkg("permission_handler", "^11.3.1")],
            android: ["RECORD_AUDIO"],
            ios: ["NSMicrophoneUsageDescription"],
            service: "audio_recording_service.dart.scriban", file: "audio_recording_service.dart"),

        Def(MobileCapabilityId.FilePicker, "Media", "File Picker", implemented: true,
            packages: [Pkg("file_picker", "^8.1.2")],
            service: "file_picker_service.dart.scriban", file: "file_picker_service.dart"),

        Def(MobileCapabilityId.ImageCompression, "Media", "Image Compression", implemented: true,
            packages: [Pkg("flutter_image_compress", "^2.3.0")],
            service: "image_compression_service.dart.scriban", file: "image_compression_service.dart"),

        Def(MobileCapabilityId.ImageCropping, "Media", "Image Cropping", implemented: true,
            packages: [Pkg("image_cropper", "^8.0.2")],
            service: "image_cropping_service.dart.scriban", file: "image_cropping_service.dart"),

        // Device
        Def(MobileCapabilityId.Biometrics, "Device", "Biometrics", implemented: true,
            packages: [Pkg("local_auth", "^2.3.0")],
            android: ["USE_BIOMETRIC", "USE_FINGERPRINT"],
            ios: ["NSFaceIDUsageDescription"],
            service: "biometric_service.dart.scriban", file: "biometric_service.dart"),

        Def(MobileCapabilityId.Notifications, "Device", "Local notifications", implemented: true,
            packages: [Pkg("flutter_local_notifications", "^18.0.1"), Pkg("permission_handler", "^11.3.1")],
            android: ["POST_NOTIFICATIONS"],
            ios: [],
            service: "notification_service.dart.scriban", file: "notification_service.dart"),

        Def(MobileCapabilityId.Contacts, "Device", "Contacts", implemented: true,
            packages: [Pkg("flutter_contacts", "^1.1.9"), Pkg("permission_handler", "^11.3.1")],
            android: ["READ_CONTACTS"],
            ios: ["NSContactsUsageDescription"],
            service: "contacts_service.dart.scriban", file: "contacts_service.dart"),

        Def(MobileCapabilityId.Calendar, "Device", "Calendar", implemented: true,
            packages: [Pkg("device_calendar", "^4.3.3"), Pkg("permission_handler", "^11.3.1")],
            android: ["READ_CALENDAR", "WRITE_CALENDAR"],
            ios: ["NSCalendarsUsageDescription"],
            service: "calendar_service.dart.scriban", file: "calendar_service.dart"),

        Def(MobileCapabilityId.Clipboard, "Device", "Clipboard", implemented: true,
            service: "clipboard_service.dart.scriban", file: "clipboard_service.dart"),

        Def(MobileCapabilityId.Share, "Device", "Share", implemented: true,
            packages: [Pkg("share_plus", "^10.0.2")],
            service: "share_service.dart.scriban", file: "share_service.dart"),

        Def(MobileCapabilityId.DeepLinks, "Device", "Deep Links", implemented: true,
            packages: [Pkg("app_links", "^6.3.2")],
            service: "deep_link_service.dart.scriban", file: "deep_link_service.dart"),

        Def(MobileCapabilityId.Vibration, "Device", "Vibration", implemented: true,
            packages: [Pkg("vibration", "^2.0.0")],
            service: "vibration_service.dart.scriban", file: "vibration_service.dart"),

        // Intelligence
        Def(MobileCapabilityId.Ocr, "Intelligence", "OCR (Text Recognition)", implemented: true,
            packages: [Pkg("google_mlkit_text_recognition", "^0.14.0")],
            service: "ocr_service.dart.scriban", file: "ocr_service.dart"),

        Def(MobileCapabilityId.BarcodeScanner, "Intelligence", "Barcode Scanner", implemented: true,
            packages: [Pkg("mobile_scanner", "^6.0.2")],
            android: ["CAMERA"],
            ios: ["NSCameraUsageDescription"],
            service: "barcode_scanner_service.dart.scriban", file: "barcode_scanner_service.dart"),

        Def(MobileCapabilityId.QrScanner, "Intelligence", "QR Scanner", implemented: true,
            packages: [Pkg("mobile_scanner", "^6.0.2")],
            android: ["CAMERA"],
            ios: ["NSCameraUsageDescription"],
            service: "qr_scanner_service.dart.scriban", file: "qr_scanner_service.dart"),

        Def(MobileCapabilityId.FaceDetection, "Intelligence", "Face Detection", implemented: true,
            packages: [Pkg("google_mlkit_face_detection", "^0.12.0")],
            android: ["CAMERA"],
            ios: ["NSCameraUsageDescription"],
            service: "face_detection_service.dart.scriban", file: "face_detection_service.dart"),

        Def(MobileCapabilityId.ObjectDetection, "Intelligence", "Object Detection", implemented: true,
            packages: [Pkg("google_mlkit_object_detection", "^0.14.0")],
            android: ["CAMERA"],
            ios: ["NSCameraUsageDescription"],
            service: "object_detection_service.dart.scriban", file: "object_detection_service.dart"),

        // Legacy / cross-layer (handled by existing templates when resolved)
        Def(MobileCapabilityId.OfflineCache, "Device", "Offline cache", "SQLite read-through cache", true,
            packages: [Pkg("sqflite", "^2.3.3"), Pkg("path", "^1.9.0"), Pkg("connectivity_plus", "^6.0.5")]),

        Def(MobileCapabilityId.SecureStorage, "Device", "Secure storage", implemented: true,
            packages: [Pkg("flutter_secure_storage", "^9.2.2")]),

        Def(MobileCapabilityId.JwtAuth, "Device", "JWT authentication", "Login when Web JWT is enabled", true)
    ];

    public static IReadOnlyList<MobileCapabilityDefinition> GetAll() => All;

    public static MobileCapabilityDefinition? TryGet(string id) =>
        All.FirstOrDefault(c => c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<MobileCapabilityDefinition> GetByCategory(string category) =>
        All.Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();

    public static IReadOnlyList<string> Categories =>
        All.Select(c => c.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static MobileCapabilityDefinition.PubspecPackage Pkg(string name, string version) =>
        new() { Name = name, Version = version };

    private static MobileCapabilityDefinition Def(
        string id,
        string category,
        string displayName,
        string? description = null,
        bool implemented = false,
        MobileCapabilityDefinition.PubspecPackage[]? packages = null,
        string[]? android = null,
        string[]? ios = null,
        string? service = null,
        string? file = null) => new()
    {
        Id = id,
        Category = category,
        DisplayName = displayName,
        Description = description,
        IsImplemented = implemented,
        PubspecPackages = packages ?? [],
        AndroidPermissions = android ?? [],
        IosPlistKeys = ios ?? [],
        ServiceTemplate = service,
        ServiceFileName = file
    };
}
