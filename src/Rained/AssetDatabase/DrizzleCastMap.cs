namespace RainEd;

static class DrizzleCastMap
{
    public static bool TryGetValue(string key, out string? value)
        => Map.TryGetValue(key, out value);
    
    private readonly static Dictionary<string, string> Map = new()
    {
        // Drought_393439_Drought Needed Init.txt
        {"4Mosaic Square", "Drought_393440_4Mosaic Square.png"},
        {"4Mosaic Slope NE", "Drought_393448_4Mosaic Slope NE.png"},
        {"4Mosaic Slope NW", "Drought_393451_4Mosaic Slope NW.png"},
        {"4Mosaic Slope SW", "Drought_393453_4Mosaic Slope SW.png"},
        {"4Mosaic Slope SE", "Drought_393452_4Mosaic Slope SE.png"},
        {"4Mosaic Floor", "Drought_393447_4Mosaic Floor.png"},
        {"3DBrick Square", "Drought_393446_3DBrick Square.png"},
        {"3DBrick Slope NE", "Drought_393442_3DBrick Slope NE.png"},
        {"3DBrick Slope NW", "Drought_393443_3DBrick Slope NW.png"},
        {"3DBrick Slope SW", "Drought_393445_3DBrick Slope SW.png"},
        {"3DBrick Slope SE", "Drought_393444_3DBrick Slope SE.png"},
        {"3DBrick Floor", "Drought_393441_3DBrick Floor.png"},
        {"AltGrateA", "Drought_393454_AltGrateA.png"},
        {"AltGrateB1", "Drought_393455_AltGrateB1.png"},
        {"AltGrateB2", "Drought_393456_AltGrateB2.png"},
        {"AltGrateB3", "Drought_393457_AltGrateB3.png"},
        {"AltGrateB4", "Drought_393458_AltGrateB4.png"},
        {"AltGrateC1", "Drought_393459_AltGrateC1.png"},
        {"AltGrateC2", "Drought_393460_AltGrateC2.png"},
        {"AltGrateE1", "Drought_393461_AltGrateE1.png"},
        {"AltGrateE2", "Drought_393462_AltGrateE2.png"},
        {"AltGrateF1", "Drought_393463_AltGrateF1.png"},
        {"AltGrateF2", "Drought_393464_AltGrateF2.png"},
        {"AltGrateF3", "Drought_393465_AltGrateF3.png"},
        {"AltGrateF4", "Drought_393466_AltGrateF4.png"},
        {"AltGrateG1", "Drought_393467_AltGrateG1.png"},
        {"AltGrateG2", "Drought_393468_AltGrateG2.png"},
        {"AltGrateH", "Drought_393469_AltGrateH.png"},
        {"AltGrateI", "Drought_393470_AltGrateI.png"},
        {"AltGrateJ1", "Drought_393471_AltGrateJ1.png"},
        {"AltGrateJ2", "Drought_393472_AltGrateJ2.png"},
        {"AltGrateJ3", "Drought_393473_AltGrateJ3.png"},
        {"AltGrateJ4", "Drought_393474_AltGrateJ4.png"},
        {"AltGrateK1", "Drought_393475_AltGrateK1.png"},
        {"AltGrateK2", "Drought_393476_AltGrateK2.png"},
        {"AltGrateK3", "Drought_393477_AltGrateK3.png"},
        {"AltGrateK4", "Drought_393478_AltGrateK4.png"},
        {"AltGrateL", "Drought_393479_AltGrateL.png"},
        {"AltGrateM", "Drought_393480_AltGrateM.png"},
        {"AltGrateN", "Drought_393481_AltGrateN.png"},
        {"AltGrateO", "Drought_393482_AltGrateO.png"},
        {"Small Stone Slope NE", "Drought_393489_Small Stone Slope NE.png"},
        {"Small Stone Slope NW", "Drought_393490_Small Stone Slope NW.png"},
        {"Small Stone Slope SW", "Drought_393492_Small Stone Slope SW.png"},
        {"Small Stone Slope SE", "Drought_393491_Small Stone Slope SE.png"},
        {"Small Stone Floor", "Drought_393488_Small Stone Floor.png"},
        {"Small Stone Marked", "Drought_393539_Small Stone Marked.png"},
        {"Square Stone Marked", "Drought_393538_Square Stone Marked.png"},
        {"Small Machine Slope NE", "Drought_393484_Small Machine Slope NE.png"},
        {"Small Machine Slope NW", "Drought_393485_Small Machine Slope NW.png"},
        {"Small Machine Slope SW", "Drought_393487_Small Machine Slope SW.png"},
        {"Small Machine Slope SE", "Drought_393486_Small Machine Slope SE.png"},
        {"Small Machine Floor", "Drought_393483_Small Machine Floor.png"},
        {"Small Metal Alt", "Drought_393493_Small Metal Alt.png"},
        {"Small Metal Marked", "Drought_393494_Small Metal Marked.png"},
        {"Small Metal X", "Drought_393495_Small Metal X.png"},
        {"Metal Floor Alt", "Drought_393500_Metal Floor Alt.png"},
        {"Metal Wall", "Drought_393502_Metal Wall.png"},
        {"Metal Wall Alt", "Drought_393501_Metal Wall Alt.png"},
        {"Square Metal Marked", "Drought_393496_Square Metal Marked.png"},
        {"Square Metal X", "Drought_393497_Square Metal X.png"},
        {"Wide Metal", "Drought_393499_Wide Metal.png"},
        {"Tall Metal", "Drought_393498_Tall Metal.png"},
        {"Big Metal X", "Drought_393508_Big Metal X.png"},
        {"Large Big Metal", "Drought_393511_Large Big Metal.png"},
        {"Large Big Metal Marked", "Drought_393509_Large Big Metal Marked.png"},
        {"Large Big Metal X", "Drought_393510_Large Big Metal X.png"},
        {"Missing Metal Slope NE", "Drought_393505_Missing Metal Slope NE.png"},
        {"Missing Metal Slope NW", "Drought_393506_Missing Metal Slope NW.png"},
        {"Missing Metal Slope SW", "Drought_393503_Missing Metal Slope SW.png"},
        {"Missing Metal Slope SE", "Drought_393507_Missing Metal Slope SE.png"},
        {"Missing Metal Floor", "Drought_393504_Missing Metal Floor.png"},
        {"Dune Sand", "Dry Editor_458767_Dune Sand.png"},
        
        // props
        {"Big Big Pipe", "Drought_393516_Big Big Pipe.png"},
        {"Ring Chain", "Drought_393512_Ring Chain.png"},
        {"Christmas Wire", "Drought_393519_Christmas Wire.png"},
        {"Ornate Wire", "Drought_393554_Ornate Wire.png"},
        {"Stretched Pipe", "Drought_393513_Stretched Pipe.png"},
        {"Twisted Thread", "Drought_393515_Twisted Thread.png"},
        {"Stretched Wire", "Drought_393514_Stretched Wire.png"},
    };
}