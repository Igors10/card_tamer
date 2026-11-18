using UnityEngine;

public static class GameData
{
    public static string nickname = ""; // typed on the play mode screen of the main menu, saved in PlayerPrefs
    public static bool randomOpponentIdentity = false;
    public static bool firstTimePlaying = true;

    public const string nicknamePrefKey = "nickname";
    public const string defaultNickname = "Player"; // nickname field on the first launch
    public const int maxNicknameLength = 16;

    static int colorIndex = -1; // player color picked last (index in CardDatabase.allPlayerColors), -1 = not loaded yet
    public const string colorPrefKey = "colorIndex";

    // online play
    public static bool hostingLobby = false; // set by the title screen, tells the lobby scene if it should host or join
    public static string joinCode = ""; // lobby code typed in the main menu, the lobby scene joins with it
    public static string menuMessage = ""; // set by the lobby when it sends a joining player back to the main menu (wrong code, host left...)
    public static int localSeat = 0; // 0 = host, 1 = guest, saved when the online match starts
    public static string[] seatNicknames = { "", "" }; // nicknames by seat, saved when the online match starts
    public static Color[] seatColors = { Color.white, Color.white }; // player colors by seat, picked in the lobby

    /// <summary>
    /// Nickname to show in game. Falls back to the saved one (when the board is started straight from the editor) and then to "Player"
    /// </summary>
    public static string GetDisplayNickname()
    {
        string name = nickname;
        if (string.IsNullOrWhiteSpace(name)) name = PlayerPrefs.GetString(nicknamePrefKey, "");
        return string.IsNullOrWhiteSpace(name) ? defaultNickname : name.Trim();
    }

    /// <summary>
    /// Player color picked last (gallery, lobby or workshop), saved in PlayerPrefs. The first color if none was picked yet
    /// </summary>
    public static int GetColorIndex(int colorCount)
    {
        if (colorIndex < 0) colorIndex = PlayerPrefs.GetInt(colorPrefKey, 0);
        return Mathf.Clamp(colorIndex, 0, colorCount - 1);
    }

    public static void SaveColorIndex(int index)
    {
        colorIndex = index;
        PlayerPrefs.SetInt(colorPrefKey, index);
    }
}
