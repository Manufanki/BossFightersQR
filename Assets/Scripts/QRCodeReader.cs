using System;
using UnityEngine;
using ZXing;
using ZXing.Common;

public class QRCodeReader : MonoBehaviour
{
    [Tooltip("Index into WebCamTexture.devices of the camera to use.")]
    [SerializeField] private int cameraIndex = 0;
    [SerializeField] private int requestedWidth = 1280;
    [SerializeField] private int requestedHeight = 720;
    [SerializeField] private int requestedFPS = 30;
    [SerializeField] private bool showWebcamView = false;
    [SerializeField] private bool enableDebugLogs = false;
    [Tooltip("Seconds between decode attempts. Lower = faster but more CPU.")]
    [SerializeField] private float decodeInterval = 0.1f;
    [Tooltip("Seconds before the same QR code can be reported again.")]
    [SerializeField] private float duplicateScanCooldown = 2f;

    private WebCamTexture _camTexture;
    private BarcodeReader _barcodeReader;
    private Color32[] _pixels;
    private Color32[] _flipped;
    private float _lastDecodeTime;
    private int _decodeAttempts;
    private int _frameCount;
    private bool _waitingForScan = true;

    public event Action<string> OnQRCodeScanned;
    public bool IsScanningEnabled { get; private set; } = true;

    private void Start()
    {
        _barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                PossibleFormats = new[] { BarcodeFormat.QR_CODE },
                TryHarder = true,
                TryInverted = true
            }
        };

        WebCamDevice[] devices = WebCamTexture.devices;
        Log($"Found {devices.Length} camera device(s).");
        for (int i = 0; i < devices.Length; i++)
        {
            Log($"Device {i}: name='{devices[i].name}' isFrontFacing={devices[i].isFrontFacing}");
        }

        if (devices.Length == 0)
        {
            Debug.LogError("[QRCodeReader] No webcam found.");
            return;
        }

        if (cameraIndex < 0 || cameraIndex >= devices.Length)
        {
            Debug.LogError($"[QRCodeReader] cameraIndex {cameraIndex} is out of range (0-{devices.Length - 1}). Falling back to 0.");
            cameraIndex = 0;
        }

        WebCamDevice device = devices[cameraIndex];
        Resolution[] available = device.availableResolutions;

        if (available == null || available.Length == 0)
        {
            // No resolution info reported: let the driver pick its native resolution instead of
            // forcing values that can silently fail to start (surfaces later as "Could not pause pControl").
            Debug.LogWarning("[QRCodeReader] No available resolutions reported by device. Using driver default resolution.");
            _camTexture = new WebCamTexture(device.name);
        }
        else
        {
            Resolution best = available[0];
            long bestDiff = long.MaxValue;
            foreach (Resolution res in available)
            {
                long diff = System.Math.Abs((long)res.width * res.height - (long)requestedWidth * requestedHeight);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = res;
                }
            }

            Log($"Device supports {available.Length} resolution(s); picked {best.width}x{best.height}@{best.refreshRateRatio.value}.");
            _camTexture = new WebCamTexture(device.name, best.width, best.height, requestedFPS);
        }

        _camTexture.Play();
        Log($"Camera started: index={cameraIndex} name={device.name} requested={requestedWidth}x{requestedHeight}");
    }

    private void Update()
    {
        if (!IsScanningEnabled || !_waitingForScan || _camTexture == null)
            return;

        if (!_camTexture.isPlaying)
        {
            Log("Camera texture is not playing.");
            return;
        }

        if (!_camTexture.didUpdateThisFrame)
            return;

        _frameCount++;

        if (Time.time - _lastDecodeTime < decodeInterval)
            return;

        _lastDecodeTime = Time.time;

        int w = _camTexture.width;
        int h = _camTexture.height;

        // Unity reports 16x16 placeholder dims until the device actually initializes.
        if (w <= 16 || h <= 16)
        {
            Log($"Camera not yet initialized (dims {w}x{h}). Waiting...");
            return;
        }

        if (_pixels == null || _pixels.Length != w * h)
        {
            _pixels = new Color32[w * h];
            _flipped = new Color32[w * h];
            Log($"Allocated pixel buffers for {w}x{h}. rotationAngle={_camTexture.videoRotationAngle} verticallyMirrored={_camTexture.videoVerticallyMirrored}");
        }

        _camTexture.GetPixels32(_pixels);

        // GetPixels32 returns rows bottom-to-top; ZXing expects top-to-bottom, so flip rows.
        for (int y = 0; y < h; y++)
        {
            System.Array.Copy(_pixels, y * w, _flipped, (h - 1 - y) * w, w);
        }

        _decodeAttempts++;

        Result result = null;
        try
        {
            result = _barcodeReader.Decode(_flipped, w, h);
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
        }

        if (result != null)
        {
            // Report the code once, then wait until GameManager re-arms for the next scan.
            _waitingForScan = false;
            Log($"QR Code found after {_decodeAttempts} attempt(s), frame {_frameCount}: {result.Text}");
            OnQRCodeScanned?.Invoke(result.Text);
        }
        else if (_decodeAttempts % 10 == 0)
        {
            Log($"No QR code found yet (attempt {_decodeAttempts}, frame {_frameCount}, {w}x{h}).");
        }
    }

    private void Log(string message)
    {
        if (enableDebugLogs)
            Debug.Log($"[QRCodeReader] {message}");
    }

    public void SetScanningEnabled(bool isEnabled)
    {
        IsScanningEnabled = isEnabled;
        if (isEnabled)
            _waitingForScan = true;
    }

    private void OnGUI()
    {
        if (!showWebcamView || _camTexture == null || !_camTexture.isPlaying)
            return;

        float screenW = Screen.width;
        float screenH = Screen.height;
        float texW = _camTexture.width;
        float texH = _camTexture.height;

        float scale = Mathf.Max(screenW / texW, screenH / texH);
        float w = texW * scale;
        float h = texH * scale;
        Rect rect = new Rect((screenW - w) * 0.5f, (screenH - h) * 0.5f, w, h);

        GUI.DrawTexture(rect, _camTexture, ScaleMode.ScaleToFit);
    }

    private void OnDestroy()
    {
        if (_camTexture != null && _camTexture.isPlaying)
            _camTexture.Stop();
    }
}
