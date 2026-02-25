using System;
using System.Collections.Generic;
using System.IO.Packaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KMPExpander.Class.SimpleKMPs;
using Newtonsoft.Json;
using LibCTR.Collections;
using KMPSections;

namespace KMPExpander.Class
{
    public static class KMPMKWExport
    {
        private static readonly bool skipUnknownObjs = true;

        private static readonly float mapScale = 1f;
        private static readonly float cameraFovScale = 1f;

        private static readonly Dictionary<Byte, string> CameraTypesDict
            = new Dictionary<Byte, string>
        {
            { 0, "Follow" },
            { 1, "Fixed" },
            { 2, "Path" },
            { 3, "Follow" },
            { 4, "FixedMoveAt" },
            { 5, "PathMoveAt" },
            { 6, "FollowPath" },

        };

        private static readonly Dictionary<Byte, Byte> AreaTypesDict = new Dictionary<Byte, Byte>
        {
            { 0, 0 }, // Camera
            { 1, 2 }, // EffectController
            { 2, 3 }, // FogController (?)
            { 3, 4 }, // PullController
            { 4, 5 }, // EnemyFall (?)
            { 11, 2 } // EffectController (? Sound Effect)
        };

        private static readonly Dictionary<ushort, string> ObjectTypesDict
           = new Dictionary<ushort, string>
        {
            { 0x0002, "lensFX" }, // ef_env_sun / Sun Effect
            { 0x0003, "sound_audience" }, // SoundPoint / Sound Effect
            { 0x0004, "itembox" }, // itemBox / Itembox
            { 0x0005, "coin" }, // CoinJmap / Coin
            { 0x0006, "karehayama" }, // WiiLeaves / Leaf Pile
            { 0x0007, "woodbox" }, // CrashBox / Wooden Box
            { 0x0008, "FlamePole_v_big" }, // GasFire / Fire Burner
            //{ 0x0009, "itembox" }, // jugem / Unused Lakitu
            //{ 0x000A, "itembox" }, // occlusion / Occlusion
            //{ 0x000B, "itembox" }, // HotSpring / Water Geyser
            //{ 0x000C, "itembox" }, // DsChandelier / Chandelier
            //{ 0x000D, "itembox" }, // Barrel1 / Barrel
            //{ 0x000E, "itembox" }, // CmnPot1 / Pot
            { 0x000F, "EnvKareha" }, // ef_maple / Falling Leaves Effect
            { 0x0010, "Psea" }, // KbWaterSurface / Koopa Troopa Beach Sea
            { 0x0011, "StarRing" }, // RrStarRing / Unused Star Ring
            //{ 0x0012, "itembox" }, // MovingRoad / Moving Road
            { 0x0013, "Psea" }, // PsWaterSurface / Palm Shore Water
            { 0x0014, "FlamePole" }, // BcFirePillar / Fire Geyser
            //{ 0x0015, "itembox" }, // MovingRoad2 / Moving Road Variant 2
            //{ 0x0016, "itembox" }, // MovingRoad1 / Moving Road Variant 1
            //{ 0x0017, "itembox" }, // CmnWaterCurrent / Water Current
            //{ 0x0018, "itembox" }, // CmnAirCurrent / Wind Variant 1
            //{ 0x0019, "itembox" }, // AcSteam / Steam
            //{ 0x001A, "itembox" }, // BcLavaFlow / Lava Stream
            //{ 0x001B, "itembox" }, // WsAnchor / Swinging Anchor
            //{ 0x001C, "itembox" }, // MeltIce / Melting Ice Platform
            //{ 0x001D, "itembox" }, // CmnAirCurrent / Wind Variant 2
            //{ 0x001E, "itembox" }, // clip / Culling Handler
            //{ 0x001F, "itembox" }, // CmnWaterCurrentBig / Big Water Current
            { 0x0020, "kinoko_ud" }, // CmnKinoko3 / Blue Mushroom
            //{ 0x0021, "itembox" }, // KbSplash / Waterfall Splash
            { 0x0065, "f_itembox" }, // itemBox / Moving Itembox
            { 0x0066, "DKrockGC" }, // CmnRock1 / Boulder
            //{ 0x0067, "itembox" }, // WiAirship / Wii Blimp
            //{ 0x0068, "itembox" }, // N64Train / Train
            //{ 0x0069, "itembox" }, // N64Balloon1 / Luigi Balloon
            //{ 0x006A, "itembox" }, // GlBalloon / Hot Air Balloon
            { 0x006B, "Twanwan" }, // DsIronBall / Pinball
            //{ 0x006C, "itembox" }, // DsBound / Pinball Bumper
            { 0x006D, "DKrockGC" }, // DsSnowball / Snowball
            { 0x006E, "CarA1" }, // WiiCarA / Parking Car Variant A
            { 0x006F, "CarA2" }, // WiiCarB / Parking Car Variant B
            //{ 0x0070, "itembox" }, // DsMovingTree / Moving Tree
            //{ 0x0071, "itembox" }, // GcTable / Moving Table
            { 0x0072, "hanachan" }, // hanachan / Wiggler
            //{ 0x0073, "itembox" }, // CruiseShip / Unused Cruise Ship
            //{ 0x0074, "itembox" }, // DkAirship / Tiki Tak Blimp
            //{ 0x0075, "itembox" }, // AcBowserShip / Bowser Blimp
            //{ 0x0076, "itembox" }, // AcFutureClown / Koopa Clown Car
            //{ 0x0077, "itembox" }, // CmnCar1 / Single Car
            //{ 0x0078, "itembox" }, // StPotSnake / Unused Pot With Snake
            { 0x0079, "DKrockGC" }, // CmnRock1 / Boulder Duplicate
            { 0x007A, "CarA3" }, // WiiCarA / Parking Car Variant A Duplicate
            //{ 0x007B, "itembox" }, // WiKoopaClown / Blue Koopa Clown Car
            { 0x007C, "Twanwan" }, // wanwan / Rolling Chain Chomp
            //{ 0x007D, "itembox" }, // IceBoundStar / Star Bumper
            //{ 0x007E, "itembox" }, // IceBoundHeart / Heart Bumper
            { 0x007F, "seagull" }, // seagull / Seagull
            { 0x00C9, "kuribo" }, // kuribo / Goomba
            { 0x00CA, "basabasa" }, // basabasa / Swooper
            { 0x00CB, "dossunc" }, // dossun / Thwomp
            { 0x00CC, "boble" }, // ef_bubble / Lava Bubble
            { 0x00CD, "crab" }, // crab / Sidestepper
            { 0x00CE, "dossunc" }, // dossunStar / Super Thwomp
            { 0x00CF, "penguin_s" }, // penguin / Penguin
            { 0x00D0, "moray" }, // moray / Eel
            { 0x00D1, "pukupuku" }, // pukupuku / Cheep Cheep
            //{ 0x00D2, "itembox" }, // killer / Unused Bullet Bill
            { 0x00D3, "choropu2" }, // poo / Rocky Wrench
            //{ 0x00D4, "itembox" }, // dinosaur / Dinosaurous
            //{ 0x00D5, "itembox" }, // ShellFish / Clam
            { 0x00D6, "kuribo" }, // TikiTak1 / Tiki Goon
            //{ 0x00D7, "itembox" }, // frogoon / Frogoon
            { 0x00D8, "kuribo" }, // kuriboBlue / Underground Goomba
            //{ 0x00D9, "itembox" }, // ShyGuyCarpet / Flying Carpet Shy Guy
            //{ 0x00DA, "itembox" }, // pot / Green Jar
            //{ 0x00DB, "itembox" }, // potSnake / Cobra Jar
            { 0x00DC, "puchi_pakkun" }, // packunFlower / Piranha Plant
            //{ 0x00DD, "itembox" }, // note / Bouncing Note
            //{ 0x00DE, "itembox" }, // bee / Stingby
            //{ 0x00DF, "itembox" }, // fishBone / Fish Bone
            { 0x00E0, "cow" }, // goat / Goat
            { 0x00E1, "pakkun_f" }, // packunMusic / Music Piranha Plant
            //{ 0x012D, "itembox" }, // Start / Start Grid
            { 0x012E, "pakkun_dokan" }, // CmnDokan1 / Pipe
            //{ 0x0130, "itembox" }, // CmnTree1 / Tree Variant 1
            { 0x0131, "kinoko_ud" }, // CmnKinoko1 / Red Mushroom
            //{ 0x0132, "itembox" }, // WaterFix / Water Fix
            //{ 0x0133, "itembox" }, // CmnTree2 / Tree Variant 2
            //{ 0x0134, "itembox" }, // WiTree1 / Wii Tree Variant 1
            //{ 0x0135, "itembox" }, // WiTree2 / Wii Tree Variant 2
            //{ 0x0136, "itembox" }, // WiTree3 / Wii Tree Variant 3
            { 0x0137, "kinoko_nm" }, // CmnKinoko2 / Green Mushroom
            //{ 0x013C, "itembox" }, // N64Tree1 / N64 Tree
            //{ 0x013D, "itembox" }, // N64Saboten / Cacti
            //{ 0x013E, "itembox" }, // N64Crossing / Train Crossing Sign
            { 0x0140, "sun" }, // Sun / Sun
            //{ 0x0141, "itembox" }, // WaterDive / Global Underwater Zone
            { 0x0143, "itembox" }, // itemBox / Alternative Itembox
            { 0x0144, "coin" }, // Coin / Alternative Coin
            //{ 0x0145, "itembox" }, // WindMill / Windmill Blades
            //{ 0x0146, "itembox" }, // RcCloud1 / Cloud
            //{ 0x0148, "itembox" }, // CmnStartGrid / Unused Start Grid
            //{ 0x0149, "itembox" }, // CmnTree4 / Tree Variant 4
            //{ 0x014A, "itembox" }, // DsSkyCastle / Unused Sky Castle
            //{ 0x014B, "itembox" }, // DsCannon / Bullet Bill Launcher
            //{ 0x014D, "itembox" }, // DsDeadTree / Dead Tree
            //{ 0x014E, "itembox" }, // DsFlipper / Flipper
            //{ 0x014F, "itembox" }, // DsDram / Slot Machine
            //{ 0x0150, "itembox" }, // DsLightBeam / Spotlight
            //{ 0x0152, "itembox" }, // DsMoon / Spooky Moon
            //{ 0x0153, "itembox" }, // DsPicture1 / Boo Portrait
            //{ 0x0155, "itembox" }, // DsTombstone / Tombstone
            //{ 0x0156, "itembox" }, // DsSpookytree / Spooky Tree
            //{ 0x0157, "itembox" }, // CmnTree3 / Tree Variant 3
            //{ 0x0159, "itembox" }, // snowman / Snowman
            { 0x015A, "parasol" }, // WiiParasol / Unused Parasol
            { 0x015B, "escalator" }, // WiiEscalator / Escalator
            //{ 0x015C, "itembox" }, // CmnPylon / Red Traffic Cone
            //{ 0x015D, "itembox" }, // CmnPylon / Blue Traffic Cone
            //{ 0x015E, "itembox" }, // CmnPylon / Yellow Traffic Cone
            //{ 0x015F, "itembox" }, // CmnTree5 / Tree Variant 5
            //{ 0x0160, "itembox" }, // WaterBox / Local Underwater Zone
            //{ 0x0161, "itembox" }, // GcTree1 / GC Tree
            { 0x0162, "tree_cannon" }, // WiiCannon / Barrel Cannon
            { 0x0165, "oilSFC" }, // SfcOil / Oil Puddle
            //{ 0x0166, "itembox" }, // BumpingFlower / Buncy Flower
            //{ 0x0168, "itembox" }, // N64Tree1 / Train Duplicate
            //{ 0x0169, "itembox" }, // CmnTree1 / Tree Variant 1 Duplicate
            //{ 0x016A, "itembox" }, // CmnTree2 / Tree Variant 2 Duplicate
            //{ 0x016B, "itembox" }, // CmnTree3 / Tree Variant 3 Duplicate
            //{ 0x016C, "itembox" }, // CmnTree4 / Tree Variant 4 Duplicate
            //{ 0x016D, "itembox" }, // CmnTree5 / Tree Variant 5 Duplicate
            //{ 0x016E, "itembox" }, // WiTree1 / Wii Tree Variant 1 Duplicate
            //{ 0x016F, "itembox" }, // WiTree2 / Wii Tree Variant 2 Duplicate
            //{ 0x0170, "itembox" }, // WiTree3 / Wii Tree Variant 3 Duplicate
            //{ 0x0171, "itembox" }, // DsDeadTree / Dead Tree Duplicate
            //{ 0x0172, "itembox" }, // DsSpookytree / Spooky Tree Duplicate
            //{ 0x0173, "itembox" }, // GcTree1 / GC Tree Duplicate
            //{ 0x0174, "itembox" }, // Dkpalm / Jungle Palm Tree
            //{ 0x0175, "itembox" }, // Dkspillar / Screaming Pillar
            //{ 0x0176, "itembox" }, // Stlamp / Street Light
            //{ 0x0177, "itembox" }, // Stpot1 / Pink Jar
            //{ 0x0178, "itembox" }, // Stpot2 / Plant Pot
            //{ 0x0179, "itembox" }, // MrBush / Unused Bush
            //{ 0x017A, "itembox" }, // IcePillar / Ice Pillar
            { 0x017B, "PeachHunsuiGC" }, // CmnFountain / Fountain
            { 0x017C, "KmoonZ" }, // Stmoon / Moon
            //{ 0x017D, "itembox" }, // Dkbarrel / DK Barrel
            //{ 0x017E, "itembox" }, // N64Deck / N64 Shortcut Ramp
            { 0x017F, "StarRing" }, // RrStarRing / Star Ring
            //{ 0x0180, "itembox" }, // TcBoard / Toad Circuit Glider Board
            { 0x0181, "oilSFC" }, // AcPuddle / Oil Puddle Variant 2
            //{ 0x0182, "itembox" }, // WiWindMill / Wind Turbine Blades
            //{ 0x0183, "itembox" }, // RrPipe / Rotating Rainbow Pipe
            { 0x0184, "taimatsu" }, // BcTorch / Torch
            //{ 0x0185, "itembox" }, // IsAnemone / Unused Anemone
            //{ 0x0186, "itembox" }, // RrMovingRoad / Unused Moving Rainbow Road
            //{ 0x0187, "itembox" }, // BcPipe / Rotating Wooden Pipe
            //{ 0x0188, "itembox" }, // RrMovingRoad / Moving Rainbow Road Variant 1
            //{ 0x0189, "itembox" }, // RrMovingRoad / Moving Rainbow Road Variant 2
            { 0x018B, "Hanabi" }, // StFireWorks / Fireworks
            //{ 0x018C, "itembox" }, // MoonBox / Moon Gravity Zone
            //{ 0x018D, "itembox" }, // UgKuriboBoard / Fake Goomba
            //{ 0x018E, "itembox" }, // UgBushBoard / Fake Bush
            { 0x018F, "MiiObj01" }, // CmnMii / Spectating Mii
            //{ 0x0190, "itembox" }, // TcBalloon / Toad Balloon
            { 0x0191, "kinoko_ud" }, // MpTambourine / Bouncy Tambourine
            //{ 0x0192, "itembox" }, // BdBoard / Big Donut Glider Board
            //{ 0x0193, "itembox" }, // RrAsteroid / Asteroid
            //{ 0x0194, "itembox" }, // UgTreeS / Unused Small Tree
            //{ 0x0195, "itembox" }, // UgTreeL / Unused Large Tree
            //{ 0x0196, "itembox" }, // UgCloud / 8-bit Cloud
            //{ 0x0197, "itembox" }, // SfcMovingRoad / Retro Moving Rainbow Road Variant 1
            //{ 0x0198, "itembox" }, // SfcMovingRoad / Retro Moving Rainbow Road Variant 2
            //{ 0x0199, "itembox" }, // SfcMovingRoad / Retro Moving Rainbow Road Variant 3
            //{ 0x019A, "itembox" }, // SfcMovingRoad / Retro Moving Rainbow Road Variant 4
            //{ 0x019B, "itembox" }, // SfcMovingRoad / Unused Retro Moving Rainbow Road
            //{ 0x019C, "itembox" }, // SfcMovingRoad / Unused Retro Moving Rainbow Road
            //{ 0x019D, "itembox" }, // SfcMovingRoad / Unused Retro Moving Rainbow Road
            //{ 0x019E, "itembox" }, // SfcMovingRoad / Unused Retro Moving Rainbow Road
            //{ 0x019F, "itembox" }, // CmnCar1 / Car Group
            //{ 0x01A0, "itembox" }, // noteJumpbox / Bouncy Note Jump Box
            //{ 0x01A1, "itembox" }, // VisualSync / Music Visual Sync Handler
            //{ 0x01A2, "itembox" }, // MpTrumpet / Trumpet
            //{ 0x01A3, "itembox" }, // MpSax / Sax
            //{ 0x01A4, "itembox" }, // MpSpeaker / Speaker
            { 0x01A5, "kinoko_ud" }, // RrJump / Rainbow Mushroom
            //{ 0x01A6, "itembox" }, // SunBig / Big Sun
            { 0x01A7, "dokan_sfc" }, // CmnDokan2 / SNES Pipe
            //{ 0x01A8, "itembox" }, // packunLight / Music Piranha Plant Light
            //{ 0x01A9, "itembox" }, // GbaBoard / GBA Bowser Castle Glide Board
            //{ 0x01AA, "itembox" }, // MpBoard / Music Glider Ramp
            //{ 0x01AB, "itembox" }, // Sunset / Sunset Sun
            //{ 0x01AC, "itembox" }, // WaterBoxNoRot / Local Underwater Zone Without Rotation
            //{ 0x01AD, "itembox" }, // MoonBoxNoRot / Moon Gravity Zone Without Rotation
            //{ 0x01AE, "itembox" }, // SfcJumpBar / SNES Bouncy Bar
            { 0x01AF, "Flash_L" }, // CmnFlash / Camera Flash
            //{ 0x0385, "itembox" }, // CmnVR1 / Skybox Variant 1
            //{ 0x0386, "itembox" }, // SfcVR1 / SNES Skybox
            //{ 0x0387, "itembox" }, // N64VR1 / N64 Skybox Variant 1
            //{ 0x0388, "itembox" }, // N64VR2 / N64 Skybox Variant 2
            //{ 0x0389, "itembox" }, // AgbVR / GBA Skybox
            //{ 0x038A, "itembox" }, // DsVR1 / DS Skybox
            //{ 0x038B, "itembox" }, // WiiVR1 / Wii Skybox
            //{ 0x038C, "itembox" }, // CmnVR2 / Skybox Variant 2
            //{ 0x038D, "itembox" }, // CmnVR3 / Skybox Variant 3
            //{ 0x038E, "itembox" }, // CmnVR4 / Skybox Variant 4
            //{ 0x038F, "itembox" }, // CmnVR5 / Skybox Variant 5
            //{ 0x0390, "itembox" }, // CmnVR6 / Skybox Variant 6
            //{ 0x0391, "itembox" }, // CmnVR7 / Skybox Variant 7
            //{ 0x0392, "itembox" }, // CmnVR8 / Skybox Variant 8
            //{ 0x0393, "itembox" }, // CmnVR9 / Skybox Variant 9
            //{ 0x0394, "itembox" }, // CmnVR10 / Skybox Variant 10
            //{ 0x0395, "itembox" }, // CmnVR11 / Skybox Variant 11
            //{ 0x0396, "itembox" }, // CmnVR12 / Skybox Variant 12
        };

        public static byte[] WriteMKWKMPJSON(this SimpleKMP mapData)
        {
            var MapObjects = mapData.Objects;
            var Routes = mapData.Routes;
            var StageInfo = mapData.StageInformation;
            var StartPositions = mapData.StartPositions;
            var RespawnPoints = mapData.RespawnPoints;
            var CannonPoints = mapData.GliderRoutes;
            var CheckPointPaths = mapData.CheckPoints;
            var ItemRoutes = mapData.ItemRoutes;
            var EnemyRoutes = mapData.EnemyRoutes;
            var Areas = mapData.Area;
            var Cameras = mapData.Camera;

            string result;
            using (var stringWriter = new StringWriter())
            {
                using (var writer = new JsonTextWriter(stringWriter))
                {
                    writer.Formatting = Newtonsoft.Json.Formatting.Indented;
                    writer.Indentation = 4;
                    writer.WriteStartObject();
                    {
                        writer.WritePropertyName("mRevision");
                        writer.WriteValue(2520);

                        writer.WritePropertyName("mOpeningPanIndex");
                        int OpeningPanIndex = 0;
                        // Get first camera marked as FirstIntro
                        for (int i = 0; i < Cameras.Entries.Count; i++)
                        {
                            if (Cameras.Entries[i].TypeID == 5)
                            {
                                OpeningPanIndex = i;
                                break;
                            }
                        }
                        writer.WriteValue(OpeningPanIndex);

                        writer.WritePropertyName("mVideoPanIndex");
                        writer.WriteValue(0); // Unused

                        writer.WritePropertyName("mStartPoints");
                        writer.WriteStartArray();
                        {
                            foreach (var entry in MapObjects.Entries)
                            {
                                if (entry.ObjectID == 0x012D)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("_");
                                    writer.WriteValue(0);

                                    writer.WritePropertyName("player_index");
                                    writer.WriteValue(0);

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, entry.Pos);

                                    writer.WritePropertyName("rotation");
                                    SerializeVector3(writer, new Vector3(entry.RotationX,
                                        entry.RotationY, entry.RotationZ));

                                    writer.WriteEndObject();
                                    break;
                                }
                            }

                            for (int i = 0; i < StartPositions.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("_");
                                writer.WriteValue(i);

                                writer.WritePropertyName("player_index");
                                writer.WriteValue(i);

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, StartPositions.Entries[i].Pos);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, new Vector3(StartPositions.Entries[i].RotationX,
                                    StartPositions.Entries[i].RotationY, StartPositions.Entries[i].RotationZ));

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mEnemyPaths");
                        writer.WriteStartArray();
                        {
                            int pointCount = 0;

                            for (int i = 0; i < EnemyRoutes.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                int prevCount = 0;
                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                foreach (var path in EnemyRoutes.Entries[i].Previous)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        prevCount++;
                                    }
                                    if (prevCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (prevCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                int nextCount = 0;
                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                foreach (var path in EnemyRoutes.Entries[i].Next)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        nextCount++;
                                    }
                                    if (nextCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (nextCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(EnemyRoutes.Entries[i].Unknown);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < EnemyRoutes.Entries[i].Entries.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("deviation");
                                    writer.WriteValue(EnemyRoutes.Entries[i].Entries[j].Scale);

                                    writer.WritePropertyName("param");
                                    writer.WriteStartArray();
                                    writer.WriteValue(EnemyRoutes.Entries[i].Entries[j].MushSettingsVal);
                                    writer.WriteValue(EnemyRoutes.Entries[i].Entries[j].DriftSettingsVal);
                                    writer.WriteValue(EnemyRoutes.Entries[i].Entries[j].ToENPTEntry().Unknown2);
                                    writer.WriteValue(EnemyRoutes.Entries[i].Entries[j].ToENPTEntry().Unknown3);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, EnemyRoutes.Entries[i].Entries[j].Pos);

                                    writer.WriteEndObject();
                                    pointCount++;
                                    if (pointCount >= 255) // MKW doesn't support more than 255 points
                                    {
                                        Console.WriteLine("WARNING: 255 Enemy Points detected. Any more will be truncated.");
                                        break;
                                    }
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();

                                if (pointCount >= 255) 
                                    break;
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mItemPaths");
                        writer.WriteStartArray();
                        {
                            int pointCount = 0;
                            for (int i = 0; i < ItemRoutes.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                int prevCount = 0;
                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                foreach (var path in ItemRoutes.Entries[i].Previous)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        prevCount++;
                                    }
                                    if (prevCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (prevCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                int nextCount = 0;
                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                foreach (var path in ItemRoutes.Entries[i].Next)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        nextCount++;
                                    }
                                    if (nextCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (nextCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < ItemRoutes.Entries[i].Entries.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("deviation");
                                    writer.WriteValue(ItemRoutes.Entries[i].Entries[j].Scale);

                                    writer.WritePropertyName("param");
                                    writer.WriteStartArray();
                                    writer.WriteValue(0);
                                    writer.WriteValue(ItemRoutes.Entries[i].Entries[j].ToITPTEntry().FlyModeVal);
                                    writer.WriteValue(0);
                                    writer.WriteValue(ItemRoutes.Entries[i].Entries[j].ToITPTEntry().PlayerScanRadiusVal);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, ItemRoutes.Entries[i].Entries[j].Pos);

                                    writer.WriteEndObject();
                                    pointCount++;
                                    if (pointCount >= 255) // MKW doesn't support more than 255 points
                                    {
                                        Console.WriteLine("WARNING: 255 Item Points detected. Any more will be truncated.");
                                        break;
                                    }
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();

                                if (pointCount >= 255)
                                    break;
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCheckPaths");
                        writer.WriteStartArray();
                        {
                            int pointCount = 0;
                            for (int i = 0; i < CheckPointPaths.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                int prevCount = 0;
                                writer.WritePropertyName("mPredecessors");
                                writer.WriteStartArray();
                                foreach (var path in CheckPointPaths.Entries[i].Previous)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        prevCount++;
                                    }
                                    if (prevCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (prevCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                int nextCount = 0;
                                writer.WritePropertyName("mSuccessors");
                                writer.WriteStartArray();
                                foreach (var path in CheckPointPaths.Entries[i].Next)
                                {
                                    if (path >= 0)
                                    {
                                        writer.WriteValue(path);
                                        nextCount++;
                                    }
                                    if (nextCount >= 6) // MKW only supports 6
                                        break;
                                }
                                //if (nextCount <= 0) // write empty value if none
                                //    writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("misc");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < CheckPointPaths.Entries[i].Entries.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("mLapCheck");
                                    writer.WriteValue(CheckPointPaths.Entries[i].Entries[j].Key);

                                    writer.WritePropertyName("mLeft");
                                    SerializeVector2(writer, new Vector2 (CheckPointPaths.Entries[i].Entries[j].LeftPointX, CheckPointPaths.Entries[i].Entries[j].LeftPointZ));

                                    writer.WritePropertyName("mRespawnIndex");
                                    writer.WriteValue(CheckPointPaths.Entries[i].Entries[j].RespawnId);

                                    writer.WritePropertyName("mRight");
                                    SerializeVector2(writer, new Vector2(CheckPointPaths.Entries[i].Entries[j].RightPointX, CheckPointPaths.Entries[i].Entries[j].RightPointZ));

                                    writer.WriteEndObject();

                                    if (pointCount >= 255) // MKW doesn't support more than 255 points
                                    {
                                        Console.WriteLine("WARNING: 255 Checkpoints detected. Any more will be truncated.");
                                        break;
                                    }
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();

                                if (pointCount >= 255)
                                    break;
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mPaths");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Routes.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("interpolation");
                                writer.WriteValue(Convert.ToInt32(Routes.Entries[i].Smooth));

                                writer.WritePropertyName("loopPolicy");
                                writer.WriteValue(Convert.ToInt32(Routes.Entries[i].Loop));

                                writer.WritePropertyName("points");
                                writer.WriteStartArray();

                                for (int j = 0; j < Routes.Entries[i].Entries.Count; j++)
                                {
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("params");
                                    writer.WriteStartArray();
                                    writer.WriteValue(Routes.Entries[i].Entries[j].Speed);
                                    writer.WriteValue(Routes.Entries[i].Entries[j].Setting2);
                                    writer.WriteEndArray();

                                    writer.WritePropertyName("position");
                                    SerializeVector3(writer, Routes.Entries[i].Entries[j].Pos);

                                    writer.WriteEndObject();
                                }
                                writer.WriteEndArray();
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mGeoObjs");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < MapObjects.Entries.Count; i++)
                            {
                                SerialiseObjTypeParams(MapObjects.Entries[i], out string objType, out int[] objSettings);

                                if (objType == "")
                                    continue; //skip objects that don't exist in mkw

                                writer.WriteStartObject();

                                writer.WritePropertyName("_");
                                writer.WriteValue(0);

                                writer.WritePropertyName("flags");
                                writer.WriteValue(63); // todo: idk

                                writer.WritePropertyName("id");
                                writer.WriteValue(objType); // todo: this

                                writer.WritePropertyName("pathId");
                                writer.WriteValue(MapObjects.Entries[i].RouteID);

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, MapObjects.Entries[i].Pos);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, new Vector3(MapObjects.Entries[i].RotationX,
                                    MapObjects.Entries[i].RotationY, MapObjects.Entries[i].RotationZ));

                                writer.WritePropertyName("scale");
                                SerializeVector3(writer, new Vector3(MapObjects.Entries[i].ScaleX,
                                    MapObjects.Entries[i].ScaleY, MapObjects.Entries[i].ScaleZ));

                                writer.WritePropertyName("settings"); // todo: this
                                writer.WriteStartArray();
                                writer.WriteValue(objSettings[0]);
                                writer.WriteValue(objSettings[1]);
                                writer.WriteValue(objSettings[2]);
                                writer.WriteValue(objSettings[3]);
                                writer.WriteValue(objSettings[4]);
                                writer.WriteValue(objSettings[5]);
                                writer.WriteValue(objSettings[6]);
                                writer.WriteValue(objSettings[7]);
                                writer.WriteEndArray();

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mAreas");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Areas.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mCameraIndex");
                                writer.WriteValue(Areas.Entries[i].CAMEIndex < 0 ? 255 : Areas.Entries[i].CAMEIndex);

                                writer.WritePropertyName("mEnemyLinkID");
                                writer.WriteValue(Areas.Entries[i].EnemyID < 0 ? 255 : Areas.Entries[i].EnemyID);

                                writer.WritePropertyName("mModel");
                                writer.WriteStartObject();
                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, Areas.Entries[i].Pos);
                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, new Vector3(Areas.Entries[i].RotationX,
                                    Areas.Entries[i].RotationY, Areas.Entries[i].RotationZ));
                                writer.WritePropertyName("mScaling");
                                SerializeVector3(writer, new Vector3(Areas.Entries[i].ScaleX,
                                    Areas.Entries[i].ScaleY, Areas.Entries[i].ScaleZ));
                                writer.WritePropertyName("mShape");
                                writer.WriteValue(Areas.Entries[i].ShapeMode == 1 ? "Cylinder" : "Box");
                                writer.WriteEndObject();

                                writer.WritePropertyName("mPad");
                                writer.WriteStartArray();
                                writer.WriteValue(0);
                                writer.WriteValue(0);
                                writer.WriteEndArray();

                                writer.WritePropertyName("mParameters");
                                writer.WriteStartArray();
                                writer.WriteValue(Areas.Entries[i].Settings1);
                                writer.WriteValue(Areas.Entries[i].Settings2);
                                writer.WriteEndArray();

                                writer.WritePropertyName("mPriority");
                                writer.WriteValue(Areas.Entries[i].Priority);

                                writer.WritePropertyName("mRailID");
                                writer.WriteValue(Areas.Entries[i].RouteID < 0 ? 255 : Areas.Entries[i].RouteID);

                                writer.WritePropertyName("mType");
                                writer.WriteValue(SerializeAreaType(Areas.Entries[i].TypeID));

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCameras");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < Cameras.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mActiveFrames");
                                writer.WriteValue(Cameras.Entries[i].DurationRaw);

                                writer.WritePropertyName("mFov");
                                writer.WriteStartObject();
                                writer.WritePropertyName("from");
                                writer.WriteValue(Cameras.Entries[i].FOVBegin);
                                writer.WritePropertyName("mSpeed");
                                writer.WriteValue(Cameras.Entries[i].FOVSpeed);
                                writer.WritePropertyName("to");
                                writer.WriteValue(Cameras.Entries[i].FOVEnd);
                                writer.WriteEndObject();

                                writer.WritePropertyName("mMovieFlag");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mNext");
                                writer.WriteValue(Cameras.Entries[i].Next < 0 ? 255 : Cameras.Entries[i].Next);

                                writer.WritePropertyName("mPathId");
                                writer.WriteValue(Cameras.Entries[i].RouteID < 0 ? 255 : Cameras.Entries[i].RouteID);

                                writer.WritePropertyName("mPathSpeed");
                                writer.WriteValue(Cameras.Entries[i].PointSpeed);

                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, Cameras.Entries[i].Pos);

                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, new Vector3(Cameras.Entries[i].RotationX,
                                    Cameras.Entries[i].RotationY, Cameras.Entries[i].RotationZ));

                                writer.WritePropertyName("mShake");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mStartFlag");
                                writer.WriteValue(0);

                                writer.WritePropertyName("mType");
                                writer.WriteValue(SerializeCameraType(Cameras.Entries[i].TypeID));

                                writer.WritePropertyName("mView");
                                writer.WriteStartObject();
                                writer.WritePropertyName("from");
                                SerializeVector3(writer, new Vector3(Cameras.Entries[i].Viewpoint1X,
                                    Cameras.Entries[i].Viewpoint1Y, Cameras.Entries[i].Viewpoint1Z));
                                writer.WritePropertyName("mSpeed");
                                writer.WriteValue(Cameras.Entries[i].ViewpointSpeed);
                                writer.WritePropertyName("to");
                                SerializeVector3(writer, new Vector3(Cameras.Entries[i].Viewpoint2X,
                                    Cameras.Entries[i].Viewpoint2Y, Cameras.Entries[i].Viewpoint2Z));
                                writer.WriteEndObject();

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mRespawnPoints");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < RespawnPoints.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("id");
                                writer.WriteValue(i);

                                writer.WritePropertyName("position");
                                SerializeVector3(writer, RespawnPoints.Entries[i].Pos);

                                writer.WritePropertyName("range");
                                writer.WriteValue(-1);

                                writer.WritePropertyName("rotation");
                                SerializeVector3(writer, new Vector3(RespawnPoints.Entries[i].RotationX,
                                    RespawnPoints.Entries[i].RotationY, RespawnPoints.Entries[i].RotationZ));

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mCannonPoints");
                        writer.WriteStartArray();
                        {
                            for (int i = 0; i < CannonPoints.Entries.Count; i++)
                            {
                                writer.WriteStartObject();

                                writer.WritePropertyName("mPosition");
                                SerializeVector3(writer, CannonPoints.Entries[i].Entries.Last().Pos);

                                writer.WritePropertyName("mRotation");
                                SerializeVector3(writer, new Vector3()); // todo: maybe try working this out idk

                                writer.WritePropertyName("mType");
                                writer.WriteValue("Direct");

                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mStages");
                        writer.WriteStartArray();
                        {
                            writer.WriteStartObject();

                            writer.WritePropertyName("_");
                            writer.WriteValue(0);

                            writer.WritePropertyName("mCorner");
                            writer.WriteValue(1); //placeholder

                            writer.WritePropertyName("mFlareTobi");
                            writer.WriteValue(1); //placeholder

                            writer.WritePropertyName("mLapCount");
                            writer.WriteValue(StageInfo.Entries[0].LapCount);

                            writer.WritePropertyName("mLensFlareOptions");
                            writer.WriteStartObject();
                            writer.WritePropertyName("a");
                            writer.WriteValue(StageInfo.Entries[0].FlareAlpha);
                            writer.WritePropertyName("b");
                            writer.WriteValue(StageInfo.Entries[0].FlareColor.B);
                            writer.WritePropertyName("g");
                            writer.WriteValue(StageInfo.Entries[0].FlareColor.G);
                            writer.WritePropertyName("r");
                            writer.WriteValue(StageInfo.Entries[0].FlareColor.R);
                            writer.WriteEndObject();

                            writer.WritePropertyName("mSpeedModifier");
                            writer.WriteValue(0);

                            writer.WritePropertyName("mStartPosition");
                            writer.WriteValue(StageInfo.Entries[0].PolePositionRaw);

                            writer.WritePropertyName("mUnk08");
                            writer.WriteValue(StageInfo.Entries[0].FlareAlpha); //placeholder (actually lensflare alpha?)

                            writer.WriteEndObject();
                        }
                        writer.WriteEnd();

                        writer.WritePropertyName("mMissionPoints");
                        writer.WriteStartArray();
                        {

                        }
                        writer.WriteEnd();

                    }
                    writer.WriteEndObject();
                }

                result = stringWriter.ToString();
            }

            return Encoding.ASCII.GetBytes(result);
        }

        public static void SerializeVector2(JsonWriter writer, Vector2 value)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.X);
            writer.WriteValue(value.Z);
            writer.WriteEndArray();
        }

        public static void SerializeVector3(JsonWriter writer, Vector3 value)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.X);
            writer.WriteValue(value.Y);
            writer.WriteValue(value.Z);
            writer.WriteEndArray();
        }

        public static string SerializeCameraType(Byte type)
        {
            if (CameraTypesDict.ContainsKey(type))
                return CameraTypesDict[type];
            else
                return "Follow";
        }

        public static byte SerializeAreaType(Byte type)
        {
            if (AreaTypesDict.ContainsKey(type))
                return AreaTypesDict[type];
            else
                return 0;
        }

        public static void SerialiseObjTypeParams(Objects.ObjectEntry mapObj, out string objName, out int[] settings)
        {
            objName = "";
            settings = new int[8];
            if (ObjectTypesDict.ContainsKey(mapObj.ObjectID))
            {
                switch (mapObj.ObjectID)
                {
                    default:
                        objName = ObjectTypesDict[mapObj.ObjectID];
                        break;
                }
            }
            else
            {
                if (!skipUnknownObjs)
                {
                    objName = mapObj.ObjectID.ToString();
                }
                else
                {
                    return;
                }
            }

            settings[0] = mapObj.Settings1;
            settings[1] = mapObj.Settings2;
            settings[2] = mapObj.Settings3;
            settings[3] = mapObj.Settings4;
            settings[4] = mapObj.Settings5;
            settings[5] = mapObj.Settings6;
            settings[6] = mapObj.Settings7;
            settings[7] = mapObj.Settings8;
        }
    }
}
