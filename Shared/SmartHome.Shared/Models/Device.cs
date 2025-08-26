namespace SmartHome.Shared {
    public class Device {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
