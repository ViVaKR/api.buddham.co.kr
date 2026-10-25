namespace Buddham.Shared.Constants;

public static class ContentTypes
{
    // ── Application ──────────────────────────────────────────────
    public static class Application
    {
        public const string Json = "application/json";
        public const string JsonPatch = "application/json-patch+json";
        public const string JsonMergePatch = "application/merge-patch+json";
        public const string JsonLd = "application/ld+json";
        public const string Xml = "application/xml";
        public const string XmlDtd = "application/xml-dtd";
        public const string Soap = "application/soap+xml";
        public const string Atom = "application/atom+xml";
        public const string Rss = "application/rss+xml";
        public const string FormUrlEncoded = "application/x-www-form-urlencoded";
        public const string OctetStream = "application/octet-stream";
        public const string Pdf = "application/pdf";
        public const string Zip = "application/zip";
        public const string GZip = "application/gzip";
        public const string Tar = "application/x-tar";
        public const string SevenZip = "application/x-7z-compressed";
        public const string Rar = "application/vnd.rar";
        public const string Bzip2 = "application/x-bzip2";
        public const string Wasm = "application/wasm";
        public const string GraphQL = "application/graphql";
        public const string Protobuf = "application/protobuf";
        public const string MsgPack = "application/msgpack";
        public const string Cbor = "application/cbor";
        public const string Sql = "application/sql";
        public const string Javascript = "application/javascript";
        public const string Jwt = "application/jwt";
        public const string PemCertificate = "application/x-pem-file";
        public const string Pkcs12 = "application/x-pkcs12";
        public const string Manifest = "application/manifest+json";
        public const string ProblemJson = "application/problem+json"; // RFC 7807
        public const string ProblemXml = "application/problem+xml";
        public const string NdJson = "application/x-ndjson"; // Newline-Delimited JSON (SSE 스트리밍)
        public const string Yaml = "application/yaml";

        // Microsoft Office
        public static class Office
        {
            public const string Word = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            public const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            public const string PowerPoint = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            public const string WordLegacy = "application/msword";
            public const string ExcelLegacy = "application/vnd.ms-excel";
            public const string PowerPointLegacy = "application/vnd.ms-powerpoint";
        }

        // OpenDocument
        public static class OpenDocument
        {
            public const string Text = "application/vnd.oasis.opendocument.text";
            public const string Spreadsheet = "application/vnd.oasis.opendocument.spreadsheet";
            public const string Presentation = "application/vnd.oasis.opendocument.presentation";
        }
    }

    // ── Text ─────────────────────────────────────────────────────
    public static class Text
    {
        public const string Plain = "text/plain";
        public const string Html = "text/html";
        public const string Xml = "text/xml";
        public const string Css = "text/css";
        public const string Javascript = "text/javascript";
        public const string Csv = "text/csv";
        public const string Markdown = "text/markdown";
        public const string Calendar = "text/calendar"; // iCal
        public const string Yaml = "text/yaml";
        public const string EventStream = "text/event-stream"; // SSE
        public const string Rtf = "text/rtf";
        public const string Vcard = "text/vcard";
        public const string Sgml = "text/sgml";
        public const string Turtle = "text/turtle"; // RDF Turtle (시맨틱 웹)
    }

    // ── Image ─────────────────────────────────────────────────────
    public static class Image
    {
        public const string Jpeg = "image/jpeg";
        public const string Png = "image/png";
        public const string Gif = "image/gif";
        public const string Webp = "image/webp";
        public const string Svg = "image/svg+xml";
        public const string Bmp = "image/bmp";
        public const string Ico = "image/x-icon";
        public const string Tiff = "image/tiff";
        public const string Avif = "image/avif";
        public const string Heic = "image/heic";
        public const string Heif = "image/heif";
    }

    // ── Audio ─────────────────────────────────────────────────────
    public static class Audio
    {
        public const string Mpeg = "audio/mpeg"; // MP3
        public const string Ogg = "audio/ogg";
        public const string Wav = "audio/wav";
        public const string Webm = "audio/webm";
        public const string Aac = "audio/aac";
        public const string Flac = "audio/flac";
        public const string Mp4 = "audio/mp4";
        public const string Opus = "audio/opus";
        public const string Midi = "audio/midi";
    }

    // ── Video ─────────────────────────────────────────────────────
    public static class Video
    {
        public const string Mp4 = "video/mp4";
        public const string Webm = "video/webm";
        public const string Ogg = "video/ogg";
        public const string Avi = "video/x-msvideo";
        public const string Mpeg = "video/mpeg";
        public const string Quicktime = "video/quicktime"; // MOV
        public const string ThreeGpp = "video/3gpp";
        public const string Matroska = "video/x-matroska"; // MKV
    }

    // ── Multipart ─────────────────────────────────────────────────
    public static class Multipart
    {
        public const string FormData = "multipart/form-data";
        public const string Mixed = "multipart/mixed";
        public const string Related = "multipart/related";
        public const string Alternative = "multipart/alternative"; // 이메일 HTML/Plain 동시 발송
        public const string Byteranges = "multipart/byteranges"; // Range 요청 응답
    }

    // ── Font ──────────────────────────────────────────────────────
    public static class Font
    {
        public const string Woff = "font/woff";
        public const string Woff2 = "font/woff2";
        public const string Ttf = "font/ttf";
        public const string Otf = "font/otf";
    }

    // ── 편의 상수 (자주 쓰는 것들 최상단 노출) ─────────────────────
    public const string Json = Application.Json;
    public const string ProblemJson = Application.ProblemJson;
    public const string FormUrlEncoded = Application.FormUrlEncoded;
    public const string FormData = Multipart.FormData;
    public const string OctetStream = Application.OctetStream;
    public const string Html = Text.Html;
    public const string Plain = Text.Plain;
    public const string EventStream = Text.EventStream; // 법등 SSE 스트리밍 ✨
}