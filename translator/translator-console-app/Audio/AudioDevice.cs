using NAudio.CoreAudioApi;

namespace translator
{
    public enum DeviceType
    {
        input,
        output
    }
    public class AudioDevice
    {
        public AudioDevice(string name, string id, MMDevice mmDevice)
        {
            Name = name;
            ID = id;
            MMDevice = mmDevice;
        }
        public string Name { get; set; }
        public string ID { get; set; }
        public MMDevice MMDevice { get; set; }
    }
}
