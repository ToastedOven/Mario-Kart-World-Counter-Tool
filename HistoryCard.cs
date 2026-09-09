using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CounterTool;

public partial class HistoryCard : Control
{
    [Export] private TextureRect card1, card2, card3, card4, comingFromCard, driver, kart, position, playerCount;
    [Export] private Label label1, label2, label3, label4;
    [Export] private Button deleteButton, reworkButton;
    public Dictionary<string, string> info = new();
    private List<string> neededKeys = new() { "Option1", "Option2", "Option3", "Picked", "Random", "ComingFrom", "NewSession", "Position", "PlayerCount", "DriverIndex", "KartIndex", "MatchVRs", "MyVR" };
    public static List<string> trackNames =
    [
        "Mario Bros Circuit",
        "Crown City",
        "Whistlestop Summit",
        "DK Spaceport",
        "Desert Hills",
        "Shy Guy Bazaar",
        "Wario Stadium",
        "Airship Fortress",
        "DK Pass",
        "Starview Peak",
        "Sky High Sundae",
        "Wario Shipyard",
        "Koopa Troopa Beach",
        "Faraway Oasis",
        "Peach Beach",
        "Salty Salty Speedway",
        "Dino Dino Jungle",
        "Great ? Block Ruins",
        "Cheep Cheep Falls",
        "Dandelion Depths",
        "Boo Cinema",
        "Dry Bones Burnout",
        "Moo Moo Meadows",
        "Choco Mountain",
        "Toads Factory",
        "Bowsers Castle",
        "Acorn Heights",
        "Mario Circuit",
        "Peach Stadium",
        "Rainbow Road",
        "SNES Mario Circuit 1",
        "SNES Mario Circuit 2",
        "SNES Mario Circuit 3",
        "SNES Ghost Valley 1",
        "SNES Ghost Valley 2",
        "SNES Ghost Valley 3",
        "SNES Choco Island 1",
        "SNES Choco Island 2",
        "SNES Vanilla Lake 1",
        "SNES Koopa Beach 1"
    ];

    private static Dictionary<int, string> racerNumbersToNames = new()
    {
        { 0, "Swoop" },
        { 1, "Para-Biddybud" },
        { 2, "Baby Peach" },
        { 3, "Baby Daisy" },
        { 4, "Spike" },
        { 5, "Goomba" },
        { 6, "Baby Mario" },
        { 7, "Peepa" },
        { 8, "Dry Bones" },
        { 9, "Baby Luigi" },
        { 10, "Sidestepper" },
        { 11, "Fish Bones" },
        { 12, "Baby Rosalina" },
        { 13, "Nabbit" },
        { 14, "Toadette" },
        { 15, "Toad" },
        { 16, "Shy Guy" },
        { 17, "Stingby" },
        { 18, "Koopa Troopa" },
        { 19, "Lakitu" },
        { 20, "Cheep Cheep" },
        { 21, "Peach" },
        { 22, "Daisy" },
        { 23, "Coin Cougher" },
        { 24, "Yoshi" },
        { 25, "Monty Mole" },
        { 26, "Dolphin" },
        { 27, "Bowser Jr" },
        { 28, "Mario" },
        { 29, "Rocky Wrench" },
        { 30, "Luigi" },
        { 31, "Pokey" },
        { 32, "Hammer Bro" },
        { 33, "Penguin" },
        { 34, "Birdo" },
        { 35, "Snowman" },
        { 36, "Piranha Plant" },
        { 37, "Pauline" },
        { 38, "King Boo" },
        { 39, "Conkdor" },
        { 40, "Rosalina" },
        { 41, "Cataquack" },
        { 42, "Wiggler" },
        { 43, "Wario" },
        { 44, "Donkey Kong" },
        { 45, "Cow" },
        { 46, "Charging Chuck" },
        { 47, "Waluigi" },
        { 48, "Pianta" },
        { 49, "Bowser" }
    };

    private static Dictionary<int, string> kartNumbersToNames = new()
    {
        { 0, "R.O.B. H.O.G." },
        { 1, "Mach Rocket" },
        { 2, "Rally Bike" },
        { 3, "Hyper Pipe" },
        { 4, "Fin Twin" },
        { 5, "Dolphin Dasher" },
        { 6, "Tune Thumper" },
        { 7, "Standard Bike" },
        { 8, "Cute Scoot" },
        { 9, "Baby Blooper" },
        { 10, "Biddybuggy" },
        { 11, "Pipe Frame" },
        { 12, "Loco Moto" },
        { 13, "Standard Kart" },
        { 14, "Plushbuggy" },
        { 15, "W-Twin Chopper" },
        { 16, "Roadster Royale" },
        { 17, "Hot Rod" },
        { 18, "Bumble V" },
        { 19, "B Dasher" },
        { 20, "Zoom Buggy" },
        { 21, "Rally Kart" },
        { 22, "Dread Sled" },
        { 23, "Ribbit Revster" },
        { 24, "Cloud 9" },
        { 25, "Carpet Flyer" },
        { 26, "Blastronaut III" },
        { 27, "Reel Racer" },
        { 28, "Funky Dorrie" },
        { 29, "Junkyard Hog" },
        { 30, "Billdozer" },
        { 31, "Big Horn" },
        { 32, "Tiny Titan" },
        { 33, "Li'l Dumpy" },
        { 34, "Chargin' Truck" },
        { 35, "Bowser Bruiser" },
        { 36, "Mecha Trike" },
        { 37, "Stellar Sled" },
        { 38, "Rallygator" },
        { 39, "Lobster Roller" }
    };

    public void ReSetup()
    {
        card1.Texture = ControlManager.instance.trackTextures[info["Option1"].ToInt() % ControlManager.TRACKCOUNT];
        card2.Texture = ControlManager.instance.trackTextures[info["Option2"].ToInt() % ControlManager.TRACKCOUNT];
        card3.Texture = ControlManager.instance.trackTextures[info["Option3"].ToInt() % ControlManager.TRACKCOUNT];
        card4.Texture = ControlManager.instance.trackTextures[info["Picked"].ToInt() % ControlManager.TRACKCOUNT];
        label1.Text = info["Option1"].ToInt() >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        label2.Text = info["Option2"].ToInt() >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        label3.Text = info["Option3"].ToInt() >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        label4.Text = info["Picked"].ToInt() >= ControlManager.TRACKCOUNT ? "Intermission" : "3Lap";
        if (info["ComingFrom"] != "null")
        {
            comingFromCard.Texture = ControlManager.instance.trackTextures[info["ComingFrom"].ToInt() % ControlManager.TRACKCOUNT];
        }
        else
        {
            comingFromCard.Texture = null;
        }

        if (info["DriverIndex"] != "null")
        {
            driver.Texture = SearchBar.instance.characters.GetChild<TextureRect>(info["DriverIndex"].ToInt()).Texture;
            kart.Texture = SearchBar.instance.karts.GetChild<TextureRect>(info["KartIndex"].ToInt()).Texture;
            position.Texture = SearchBar.instance.positions.GetChild<TextureRect>(info["Position"].ToInt() - 1).Texture;
            playerCount.Texture = SearchBar.instance.positions.GetChild<TextureRect>(info["PlayerCount"].ToInt() - 1).Texture;
        }
        else
        {
            driver.Texture = null;
            kart.Texture = null;
            position.Texture = null;
            playerCount.Texture = null;
        }
    }
    public void Setup(string cardData)
    {
        var rawInfo = cardData.Split(",");
        info = new();
        foreach (var infoItem in rawInfo)
        {
            info.Add(infoItem.Split(">>")[0], infoItem.Split(">>")[1]);
        }
        foreach (var neededKey in neededKeys)
        {
            if (!info.ContainsKey(neededKey))
            {
                info.Add(neededKey, "null");
            }
        }
        ReSetup();
    }

    public string GetDataForSaving()
    {
        StringBuilder sb = new();
        foreach (var neededKey in neededKeys)
        {
            if (!info.ContainsKey(neededKey))
            {
                info.Add(neededKey, "null");
            }
        }
        foreach (var key in info.Keys)
        {
            sb.Append($"{key}>>{info[key]},");
        }

        return sb.ToString().TrimEnd(',');
    }

    public HistoryEntry GetHistoryInfoForDb()
    {
        List<int> vrs = new();
        int myVR = -1;
        if (info["MatchVRs"] != "null")
        {
            myVR = info["MyVR"].ToInt();
            foreach (var vr in info["MatchVRs"].Split('?'))
            {
                vrs.Add(int.Parse(vr));
            }   
        }
        else
        {
            vrs.Add(-1);
        }
        //option 123 and picked are "comingfrom to thing" if intermission, otherwise, just "thing"
        string comingFrom = trackNames[info["ComingFrom"].ToInt() % ControlManager.TRACKCOUNT];
        var newMatch = new HistoryEntry
        {
            Option1 = Thingy(comingFrom, "Option1"),
            Option2 = Thingy(comingFrom, "Option2"),
            Option3 = Thingy(comingFrom, "Option3"),
            Picked = Thingy(comingFrom, "Picked"),
            Random = info["Random"] == "1",
            ComingFrom = comingFrom,
            NewSession = info["NewSession"] == "1",
            Placement = info["Position"].ToInt(),
            PlayerCount = info["PlayerCount"].ToInt(),
            Racer = racerNumbersToNames[info["DriverIndex"].ToInt()],
            Kart = kartNumbersToNames[info["KartIndex"].ToInt()],
            MyVr = myVR,
            MatchVrs = vrs,
            Option1Votes = info["Option1Votes"].ToInt(),
            Option2Votes = info["Option2Votes"].ToInt(),
            Option3Votes = info["Option3Votes"].ToInt(),
            RandomVotes = info["RandomVotes"].ToInt(),
            Timestamp = long.Parse(info["Timestamp"]),
            Disconnect = info["Disconnected"] == "1",
            Version = info["Version"],
            Mirror = info["Mirror"] == "1",
            
        };
        return newMatch;
    }

    private string Thingy(string comingFrom, string option)
    {
        string option1;
        if (info[option].ToInt() < ControlManager.TRACKCOUNT)
        {
            option1 = trackNames[info[option].ToInt()];
        }
        else
        {
            option1 = $"{comingFrom} >>> {trackNames[info[option].ToInt() % ControlManager.TRACKCOUNT]}";
        }

        return option1;
    }

    public override void _Ready()
    {
        deleteButton.Pressed += DeleteButtonOnPressed;
        reworkButton.Pressed += ReworkButtonOnPressed;
    }

    private void ReworkButtonOnPressed()
    {
        VerifyDataPage.instance.SetupHistoryCard(this);
    }

    private void DeleteButtonOnPressed()
    {
        HistoryHandler.cards.Remove(this);
        QueueFree();
        SaveManager.Save();
    }
}
