using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

public class UnityHttpListener : MonoBehaviour
{
    private HttpListener _listener;
    private Thread _listenerThread;
    private Camera _camera;
    private Vector3? _nextCameraPosition;
    private float? _nextCameraHeading;
    [CanBeNull] private IEnumerator _takeScreenShotCoroutine;
    [CanBeNull] private Move _nextMove;

    void Start()
    {
        Application.targetFrameRate = 10;
        _camera = GetComponent<Camera>();

        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:4444/");
        _listener.Prefixes.Add("http://127.0.0.1:4444/");
        _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        _listener.Start();

        _listenerThread = new Thread(StartListener);
        _listenerThread.Start();
        Debug.Log("Server Started");
    }

    void Update()
    {

        if (_nextCameraPosition != null)
        {
            _camera.transform.position = (Vector3) _nextCameraPosition;
            _nextCameraPosition = null;
        }
        if (_nextCameraHeading != null)
        {
            _camera.transform.rotation = Quaternion.Euler(0, (float) _nextCameraHeading, 0);
            _nextCameraHeading = null;
        }
        if (_nextMove != null)
        {
            _camera.transform.Translate(_camera.transform.forward * _nextMove.forward);
            _camera.transform.Translate(_camera.transform.up * _nextMove.up);
            _camera.transform.Translate(_camera.transform.right * _nextMove.right);
            _nextMove = null;
        }
    }

    void LateUpdate()
    {
        if (_takeScreenShotCoroutine != null)
        {
            var coroutine = _takeScreenShotCoroutine;
            _takeScreenShotCoroutine = null;
            StartCoroutine(coroutine);
        }
    }

    private void StartListener()
    {
        while (true)
        {
            var result = _listener.BeginGetContext(ListenerCallback, _listener);
            result.AsyncWaitHandle.WaitOne();
        }
    }

    private void ListenerCallback(IAsyncResult result)
    {
        var context = _listener.EndGetContext(result);

        Debug.Log("Method: " + context.Request.HttpMethod);
        Debug.Log("LocalUrl: " + context.Request.Url.LocalPath);

        try
        {
            HandleRequest(context);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        if (context.Request.HttpMethod == "POST")
        {
            var dataText = new StreamReader(context.Request.InputStream,
                context.Request.ContentEncoding).ReadToEnd();
            Debug.Log(dataText);

            Command command;
            try
            {
                command = JsonUtility.FromJson<Command>(dataText);
            }
            catch (Exception e)
            {
                Debug.Log("Bad request" + e);
                return;
            }

            Debug.Log("Command type:" + command.commandType);
            
            switch (Enum.Parse(typeof(CommandType), command.commandType))
            {
                case CommandType.CameraPosition:
                    var c = JsonUtility.FromJson<CameraPosition>(dataText);
                    Debug.Log("Received message:");
                    Debug.Log(c);
                    Debug.Log("x:" + c.x + " y:" + c.y + " height:" + c.height + " heading:" + c.heading);
                    _nextCameraPosition = new Vector3(c.x, c.height, c.y);
                    _nextCameraHeading = c.heading;
                    break;
                case CommandType.Move:
                    var move = JsonUtility.FromJson<Move>(dataText);
                    Debug.Log("Received message:");
                    Debug.Log(move);
                    Debug.Log("forward:" + move.forward + " sideways:" + move.right + " upward:" + move.up + " rotate:" + move.rotate);

                    _nextMove = move;
                    break;
                case CommandType.TakeScreenshot:
                    Debug.Log("Taking screenshot");
                    var screenshotPath = GenerateScreenshotPath();
                    _takeScreenShotCoroutine = TakeScreenShotCoroutine(screenshotPath);

                    // Wait for screenshot
                    while (!File.Exists(screenshotPath))
                    {
                        Thread.Sleep(50);
                    }
                    WriteHttpResponse(context.Response, screenshotPath);
                    
                    break;
                default:
                    Debug.Log("Unknown command type");
                    return;
            }
        }
        Debug.Log("Responded to HTTP call");
    }

    private IEnumerator TakeScreenShotCoroutine(string filename)
    {
        yield return new WaitForEndOfFrame();
        Debug.Log(filename);
        ScreenCapture.CaptureScreenshot(filename);
    }

    private static string GenerateScreenshotPath()
    {
        string folderPath = Directory.GetCurrentDirectory() + "/Screenshots/";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var screenshotName = "Screenshot_" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + ".png";

        var filename = Path.Combine(folderPath, screenshotName);
        return filename;
    }

    private static void WriteHttpResponse(HttpListenerResponse response, string payload)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(payload);
        // Get a response stream and write the response to it.
        response.ContentLength64 = buffer.Length;
        Stream output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();
    }
}