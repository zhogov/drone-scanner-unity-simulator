using System;

[Serializable]
public enum CommandType 
{
    CameraPosition,
    Move,
    TakeScreenshot,
}

[Serializable]
public class Command
{
    public string commandType;
}
    
[Serializable]
public class CameraPosition
{
    public float x;
    public float y;
    public float height;
    public float heading;
}    

[Serializable]
public class Move
{
    public float forward;
    public float right;
    public float up;
    public float rotate;
}