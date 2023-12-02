public class CommandPayload
{
    public string name { get; set; }
    public string description { get; set; }
    public Option[] options { get; set; }
}

public class Option
{
    public string name { get; set; }
    public string description { get; set; }
    public int type { get; set; } // 3 is for STRING type option
    public bool required { get; set; }
    public Choice[] choices { get; set; }
}

public class Choice
{
    public string name { get; set; }
    public string value { get; set; }
}
