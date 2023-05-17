using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Controls;

namespace Client.Scripts
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    internal class Audio
    {
        private MediaCapture mediaCapture;
        private MediaFrameReader mediaFrameReader;
        private InMemoryRandomAccessStream memoryBuffer;
        private AudioFrameInputNode AudioFrameInput;

        private Audio() { }

        public static async Task<Audio> CreateAsync()
        {
            var instance = new Audio();

            Windows.Devices.Enumeration.DeviceInformationCollection devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Media.Devices.MediaDevice.GetAudioCaptureSelector());

            System.Diagnostics.Debug.WriteLine(devices.Count);

            MediaCaptureInitializationSettings settings =
            new MediaCaptureInitializationSettings
            {
                AudioDeviceId = Windows.Media.Devices.MediaDevice.GetDefaultAudioRenderId(Windows.Media.Devices.AudioDeviceRole.Default),
                StreamingCaptureMode = StreamingCaptureMode.Audio,
                SharingMode = MediaCaptureSharingMode.SharedReadOnly
            };
            instance.mediaCapture = new MediaCapture();

            await instance.mediaCapture.InitializeAsync(settings);

            var audioFrameSources = instance.mediaCapture.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio);

            MediaFrameSource frameSource = audioFrameSources.FirstOrDefault().Value;

            instance.mediaFrameReader = await instance.mediaCapture.CreateFrameReaderAsync(frameSource);

            return instance;
        }

        public async void Start()
        {
            mediaFrameReader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Buffered;

            mediaFrameReader.FrameArrived += MediaFrameReader_AudioFrameArrived;

            await mediaFrameReader.StartAsync();
        }

        private void MediaFrameReader_AudioFrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("asfrewdgsdewg");
            using (MediaFrameReference reference = sender.TryAcquireLatestFrame())
            {
                if (reference != null)
                {
                    ProcessAudioFrame(reference.AudioMediaFrame.GetAudioFrame());
                }
            }
        }

        unsafe private void ProcessAudioFrame(AudioFrame frame)
        {
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            {
                Byte[] audioBytes = new byte[buffer.Length];

                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    byte* dataInBytes;
                    uint capacityInBytes;

                    // Get the buffer from the AudioFrame
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                    fixed (byte* buf = audioBytes)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            buf[i] = dataInBytes[i];
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine(audioBytes.Length);
            }
        }

        public void AddFrame(Byte[] datas)
        {
            AudioFrame frame = new AudioFrame((uint)datas.Length);

            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            {

                using (IMemoryBufferReference reference = buffer.CreateReference())
                {
                    unsafe
                    {
                        byte* dataInBytes;
                        uint capacityInBytes;

                        // Get the buffer from the AudioFrame
                        ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);


                        fixed (byte* buf = datas)
                        {
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                dataInBytes[i] = buf[i];
                            }
                        }
                    }
                }
            }

            AudioFrameInput.AddFrame(frame);
        }

        public async void Stop()
        {
            await mediaCapture.StopRecordAsync();
        }
    }

}
