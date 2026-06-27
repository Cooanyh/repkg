namespace RePKG.Application
{
    public static class RuntimeSafetySettings
    {
        public static int MaximumFrameCount { get; set; } = 100_000;
        public static int MaximumImageCount { get; set; } = 100;
        public static int MaximumMipmapCount { get; set; } = 32;

        // Video textures can legitimately be very large.
        public static int MaximumMipmapByteCount { get; set; } = 1_000_000_000;
    }
}
