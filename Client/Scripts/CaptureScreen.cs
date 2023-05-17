using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;
using System.Diagnostics;
using Windows.Graphics.Display;

namespace Client.Scripts
{
    internal class CaptureScreen
    {
        // Capture API objects.
        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        // Non-API related members.
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;
        private ClientTCP ClientTCP;

        private Composter composter;

        private SizeInt32 size = new SizeInt32();


        private static int _frameCount = 0;
        private static int _lastFrameCount = 0;

        private static DateTime _lastFrameTime;
        private static Rectangle _frameBounds;
        private const DirectXPixelFormat directXPixelFormat = DirectXPixelFormat.B8G8R8A8UIntNormalized; //B8G8R8A8UIntNormalized

        public Stream Buffer
        {
            get
            {
                return null;
            }
            set
            {
                ByteToImg(value);
            }
        }

        public CaptureScreen(Rectangle rectangle, ClientTCP clientTCP)
        {
            _frameBounds = rectangle;
            ClientTCP = clientTCP;
            Setup();
        }

        public string GetFrameCount()
        {
            return _lastFrameCount.ToString();
        }

        public void SetSize(SizeInt32 size)
        {
            this.size = size;
            composter = new Composter(size.Width, size.Height);
        }

        private void ByteToImg(Stream buf)
        {
            try
            {
                FillSurfaceWithBitmap(CanvasBitmap.CreateFromBytes(_canvasDevice,
                                        composter.Decompress(buf),
                                        size.Width,
                                        size.Height,
                                        directXPixelFormat)
                );

            }
            catch
            {
                Debug.WriteLine("errordec");
            }
        }

        private void Setup()
        {
            _canvasDevice = new CanvasDevice();

            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(
                Window.Current.Compositor,
                _canvasDevice);

            _compositor = Window.Current.Compositor;

            _surface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Size(100, 100),
                directXPixelFormat,
                DirectXAlphaMode.Premultiplied);

            var visual = _compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;
            var brush = _compositor.CreateSurfaceBrush(_surface);
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;
            visual.Brush = brush;
            ElementCompositionPreview.SetElementChildVisual(_frameBounds, visual);
        }

        public void StartCapture(int Display)
        {
            //var accessResult = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Programmatic);
            //if (accessResult == Windows.Security.Authorization.AppCapabilityAccess.AppCapabilityAccessStatus.Allowed)
            //{
                var displays = DisplayServices.FindAll();
                GraphicsCaptureItem item = GraphicsCaptureItem.TryCreateFromDisplayId(displays[Display]);
                _lastFrameTime = DateTime.Now;
                StartCaptureInternal(item);
            //}
        }

        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
            StopCapture();

            size = item.Size;

            _item = item;
            _lastSize = size;
            _framePool = Direct3D11CaptureFramePool.Create(
                _canvasDevice, // D3D device
                directXPixelFormat, // Pixel format B8G8R8A8UIntNormalized - standart
                1, // Number of frames
                size); // Size of the buffers

            composter = new Composter(size.Width, size.Height);

            _framePool.FrameArrived += (s, a) =>
            {
                using (var frame = s.TryGetNextFrame())
                {
                    ProcessFrame(frame);
                }
            };

            _item.Closed += (s, a) =>
            {
                StopCapture();
            };

            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        public void StopCapture()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        private void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            bool needsReset = false;
            bool recreateDevice = false;

            if ((frame.ContentSize.Width != _lastSize.Width) ||
                (frame.ContentSize.Height != _lastSize.Height))
            {
                needsReset = true;
                _lastSize = frame.ContentSize;
            }

            try
            {
                byte[] data = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, frame.Surface).GetPixelBytes();
                //Debug.WriteLine(data.Length);

                composter.DistributedCompression(data, ClientTCP.SendMassage);

                //int normallFPS = 20;
                if (DateTime.Now.Second - _lastFrameTime.Second != 0)
                {
                    _lastFrameTime = DateTime.Now;
                    _lastFrameCount = _frameCount;
                    //Debug.WriteLine(_lastFrameCount);
                    //if (_lastFrameCount < normallFPS - 5)
                    //    composter.type = (byte)(composter.type << 1);
                    //else if (_lastFrameCount > normallFPS + 5)
                    //    composter.type = (byte)((composter.type >> 1) | composter.type);
                    _frameCount = 0;
                }
                _frameCount++;
            }

            catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
            {
                needsReset = true;
                recreateDevice = true;
            }

            if (needsReset)
            {
                ResetFramePool(frame.ContentSize, recreateDevice);
            }
        }

        private void FillSurfaceWithBitmap(CanvasBitmap canvasBitmap)
        {
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
                session.DrawImage(canvasBitmap);
            }
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        _canvasDevice = new CanvasDevice();
                    }

                    _framePool.Recreate(
                        _canvasDevice,
                        directXPixelFormat,
                        1,
                        size);
                }
                // This is the device-lost convention for Win2D.
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice = null;
                    recreateDevice = true;
                }
            } while (_canvasDevice == null);
        }
    }
}