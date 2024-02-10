using Drizzle.Lingo.Runtime;
using Serilog;

namespace Drizzle.Ported;

//
// Movie script: comEditorUtils
//
public sealed partial class MovieScript
{
    public void clearlogs()
    {
    }

    public void writeexception(string tp = null, object msg = null)
    {
        Log.Error($"Lingo: {tp}\n{msg}");
    }

    public void writemessage(object msg = null)
    {
        Log.Debug($"Lingo: {msg}");
    }

    public void writeinfomessage(object msg = null)
    {
        Log.Information($"Lingo: {msg}");
    }

    public void writeinternalmessage(object msg = null)
    {
        Log.Verbose($"Lingo: {msg}");
    }

    public void outputinternallog()
    {
    }

    public void exportall()
    {
    }

    public LingoNumber getboolconfig(dynamic str = default)
    {
        string txt = _global.member(@"editorConfig").text;
        for (int i = 1; i <= LingoGlobal.thenumberoflines_helper(txt); i++)
        {
            if (LingoGlobal.op_eq_b(LingoGlobal.linemember_helper(txt)[i], LingoGlobal.concat(str, @" : TRUE")))
            {
                return LingoGlobal.TRUE;
            }
        }

        return LingoGlobal.FALSE;
    }

    public string getstringconfig(dynamic str = default)
    {
        string txt = _global.member(@"editorConfig").text;
        for (int i = 1; i <= LingoGlobal.thenumberoflines_helper(txt); i++)
        {
            if (LingoGlobal.op_eq_b(LingoGlobal.linemember_helper(txt)[q], LingoGlobal.concat(str, @" : DROUGHT")))
            {
                return @"DROUGHT";
            }
            else if (LingoGlobal.op_eq_b(LingoGlobal.linemember_helper(txt)[q], LingoGlobal.concat(str, @" : DRY")))
            {
                return @"DRY";
            }
        }
        return @"VANILLA";

    }

    public LingoNumber checkexitrender()
    {
        return LingoGlobal.FALSE;
    }

    public LingoNumber checkexit()
    {
        return LingoGlobal.FALSE;
    }

    public LingoNumber checkdrinternal(dynamic nm = default)
    {
        return LingoGlobal.op_gt(DRInternalList.getpos(nm), new LingoNumber(0));
    }

    public void setfirsttilecat(dynamic num = default)
    {
        DRFirstTileCat = num;
    }

    public dynamic getfirsttilecat()
    {
        return DRFirstTileCat;
    }

    public void setlastmatcat(dynamic num = default)
    {
        DRLastMatCat = num;
    }

    public dynamic getlastmatcat()
    {
        return DRLastMatCat;
    }

    public dynamic initdrinternal()
    {
        DRInternalList = new LingoList { @"SGFL", @"tileSetAsphaltFloor", @"tileSetStandardFloor", @"tileSetBigMetalFloor", @"tileSetBricksFloor", @"tileSetCliffFloor", @"tileSetConcreteFloor", @"tileSetNon-Slip MetalFloor", @"tileSetRainstoneFloor", @"tileSetRough RockFloor", @"tileSetScaffoldingDRFloor", @"tileSetSteelFloor", @"tileSetSuperStructure2Floor", @"tileSetSuperStructureFloor", @"tileSetTiny SignsFloor", @"tileSetElectricMetalFloor", @"tileSetCageGrateFloor", @"tileSetGrateFloor", @"tileSetBulkMetalFloor", @"tileSetMassiveBulkMetalFloor", @"4Mosaic Square", @"4Mosaic Slope NE", @"4Mosaic Slope SE", @"4Mosaic Slope NW", @"4Mosaic Slope SW", @"4Mosaic Floor", @"3DBrick Square", @"3DBrick Slope NE", @"3DBrick Slope SE", @"3DBrick Slope NW", @"3DBrick Slope SW", @"3DBrick Floor", @"Small Stone Slope NE", @"Small Stone Slope SE", @"Small Stone Slope NW", @"Small Stone Slope SW", @"Small Stone Floor", @"Small Machine Slope NE", @"Small Machine Slope SE", @"Small Machine Slope NW", @"Small Machine Slope SW", @"Small Machine Floor", @"Missing Metal Slope NE", @"Missing Metal Slope SE", @"Missing Metal Slope NW", @"Missing Metal Slope SW", @"Missing Metal Floor", @"Small Stone Marked", @"Square Stone Marked", @"Small Metal Alt", @"Small Metal Marked", @"Small Metal X", @"Metal Floor Alt", @"Metal Wall", @"Metal Wall Alt", @"Square Metal Marked", @"Square Metal X", @"Wide Metal", @"Tall Metal", @"Big Metal X", @"Large Big Metal", @"Large Big Metal Marked", @"Large Big Metal X", @"AltGrateA", @"AltGrateB1", @"AltGrateB2", @"AltGrateB3", @"AltGrateB4", @"AltGrateC1", @"AltGrateC2", @"AltGrateE1", @"AltGrateE2", @"AltGrateF1", @"AltGrateF2", @"AltGrateF3", @"AltGrateF4", @"AltGrateG1", @"AltGrateG2", @"AltGrateH", @"AltGrateI", @"AltGrateF2", @"AltGrateJ1", @"AltGrateJ2", @"AltGrateJ3", @"AltGrateJ4", @"AltGrateK1", @"AltGrateK2", @"AltGrateK3", @"AltGrateK4", @"AltGrateL", @"AltGrateM", @"AltGrateN", @"AltGrateO", @"Big Big Pipe", @"Ring Chain", @"Stretched Pipe", @"Stretched Wire", @"Twisted Thread", @"Christmas Wire", @"Ornate Wire", @"Dune Sand" };
        RandomMetals_grabTiles = new LingoList { @"Metal", @"Metal construction", @"Plate" };
        RandomMetals_allowed = new LingoList { @"Small Metal", @"Metal Floor", @"Square Metal", @"Big Metal", @"Big Metal Marked", @"C Beam Horizontal AA", @"C Beam Horizontal AB", @"C Beam Vertical AA", @"C Beam Vertical BA", @"Plate 2" };
        ChaoticStone2_needed = new LingoList { @"Small Stone", @"Square Stone", @"Tall Stone", @"Wide Stone", @"Big Stone", @"Big Stone Marked" };
        DRRandomMetal_needed = new LingoList { @"Small Metal", @"Metal Floor", @"Square Metal", @"Big Metal", @"Big Metal Marked", @"Four Holes", @"Cross Beam Intersection" };
        SmallMachines_grabTiles = new LingoList { @"Machinery", @"Machinery2", @"Small machine" };
        SmallMachines_forbidden = new LingoList { @"Feather Box - W", @"Feather Box - E", @"Piston Arm", @"Vertical Conveyor Belt A", @"Ventilation Box Empty", @"Ventilation Box", @"Big Fan", @"Giant Screw", @"Compressor Segment", @"Compressor R", @"Compressor L", @"Hub Machine", @"Pole Holder", @"Sky Box", @"Conveyor Belt Wheel", @"Piston Top", @"Piston Segment Empty", @"Piston Head", @"Piston Segment Filled", @"Piston Bottom", @"Piston Segment Horizontal A", @"Piston Segment Horizontal B", @"machine box C_E", @"machine box C_W", @"machine box C_Sym", @"Machine Box D", @"machine box B", @"Big Drill", @"Elevator Track", @"Conveyor Belt Covered", @"Conveyor Belt L", @"Conveyor Belt R", @"Conveyor Belt Segment", @"Dyson Fan", @"Metal Holes", @"valve", @"Tank Holder", @"Drill Rim", @"Door Holder R", @"Door Holder L", @"Drill B", @"machine box A", @"Machine Box E L", @"Machine Box E R", @"Drill Shell A", @"Drill Shell B", @"Drill Shell Top", @"Drill Shell Bottom", @"Pipe Box R", @"Pipe Box L" };
        RandomMachines_grabTiles = new LingoList { @"Machinery", @"Machinery2", @"Small machine", @"Drought Machinery", @"Custom Random Machines" };
        RandomMachines_forbidden = new LingoList { @"Feather Box - W", @"Feather Box - E", @"Piston Arm", @"Vertical Conveyor Belt A", @"Piston Head No Cage", @"Conveyor Belt Holder Only", @"Conveyor Belt Wheel Only", @"Drill Valve" };
        RandomMachines2_grabTiles = new LingoList { @"Machinery", @"Machinery2", @"Small machine" };
        RandomMachines2_forbidden = new LingoList { @"Feather Box - W", @"Feather Box - E", @"Piston Arm", @"Vertical Conveyor Belt A", @"Ventilation Box Empty", @"Ventilation Box", @"Big Fan", @"Giant Screw", @"Compressor Segment", @"Compressor R", @"Compressor L", @"Hub Machine", @"Pole Holder", @"Sky Box", @"Conveyor Belt Wheel", @"Piston Top", @"Piston Segment Empty", @"Piston Head", @"Piston Segment Filled", @"Piston Bottom", @"Piston Segment Horizontal A", @"Piston Segment Horizontal B", @"machine box C_E", @"machine box C_W", @"machine box C_Sym", @"Machine Box D", @"machine box B", @"Big Drill", @"Elevator Track", @"Conveyor Belt Covered", @"Conveyor Belt L", @"Conveyor Belt R", @"Conveyor Belt Segment", @"Dyson Fan" };

        return default;
    }
}

