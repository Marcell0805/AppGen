namespace AppGen.Core.Capabilities;

public static class MobileCapabilityId
{
    // Connectivity
    public const string Internet = "internet";
    public const string Bluetooth = "bluetooth";
    public const string Nfc = "nfc";
    public const string Wifi = "wifi";

    // Location
    public const string Gps = "gps";
    public const string Maps = "maps";
    public const string Geofencing = "geofencing";
    public const string PlaceSearch = "placeSearch";

    // Media
    public const string Camera = "camera";
    public const string Gallery = "gallery";
    public const string VideoRecording = "videoRecording";
    public const string AudioRecording = "audioRecording";
    public const string FilePicker = "filePicker";
    public const string ImageCompression = "imageCompression";
    public const string ImageCropping = "imageCropping";

    // Device
    public const string Biometrics = "biometrics";
    public const string Notifications = "notifications";
    public const string Contacts = "contacts";
    public const string Calendar = "calendar";
    public const string Clipboard = "clipboard";
    public const string Share = "share";
    public const string DeepLinks = "deepLinks";
    public const string Vibration = "vibration";

    // Intelligence
    public const string Ocr = "ocr";
    public const string BarcodeScanner = "barcodeScanner";
    public const string QrScanner = "qrScanner";
    public const string FaceDetection = "faceDetection";
    public const string ObjectDetection = "objectDetection";

    // Mapped from legacy toggles / cross-layer
    public const string OfflineCache = "offlineCache";
    public const string SecureStorage = "secureStorage";
    public const string JwtAuth = "jwtAuth";
}
