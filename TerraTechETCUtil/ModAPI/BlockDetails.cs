using System;

namespace TerraTechETCUtil
{
    public struct BlockDetails
    {
        public enum Flags
        {
            None = 0,
            Boosters = 1,
            Fans = 2,
            Wings = 4,
            Hovers = 8,
            Wheels = 16,
            Gyro = 32,
            AntiGrav = 64,
            OmniDirectional = 128,
            Weapon = 256,
            Melee = 512,
            Short = 1024,
            Bubble = 2048,
            Floats = 4096,
            ControlsBlockman = 8192,
            UNUSED_Crafting = 16384,
            UNUSED_unset = 32768,
        }


        public Flags flags;
        public BlockAttributes attributesHash;

        public BlockDetails(BlockTypes BT)
        {
            flags = default;
            attributesHash = default;
            BlockIndexer.GetBlockDetails_Internal(BT, ref this);
        }
        public bool IsBasic => flags == 0 && attributesHash == 0;
        private const Flags MovementFlags = Flags.Boosters | Flags.Fans | 
            Flags.Wings | Flags.Hovers |  Flags.Wheels | Flags.Gyro | 
            Flags.AntiGrav | Flags.OmniDirectional;
        public bool DoesMovement => flags.HasAnyFlag(MovementFlags);
        private const Flags RotateFlags = Flags.Boosters |
            Flags.Fans | Flags.Wings | Flags.Hovers |
            Flags.Wheels | Flags.Gyro | Flags.OmniDirectional;
        public bool CanRotateTech => flags.HasAnyFlag(RotateFlags);
        private const Flags PushFlags = Flags.Boosters |
            Flags.Fans | Flags.Hovers | Flags.Wheels | Flags.OmniDirectional;
        public bool CanPushTech => flags.HasAnyFlag(PushFlags);
        private const Flags FloatControlFlags = Flags.Boosters |
            Flags.Fans | Flags.Wings | Flags.Hovers | Flags.OmniDirectional;
        public bool CanWorkOffGround => flags.HasAnyFlag(FloatControlFlags);

        public bool HasBoosters => flags.HasFlag(Flags.Boosters);
        public bool HasFans => flags.HasFlag(Flags.Fans);
        public bool HasWings => flags.HasFlag(Flags.Wings);
        public bool HasHovers => flags.HasFlag(Flags.Hovers);
        public bool HasAntiGravity => flags.HasFlag(Flags.AntiGrav);
        public bool IsOmniDirectional => flags.HasFlag(Flags.OmniDirectional);
        public bool IsBubble => flags.HasFlag(Flags.Bubble);
        public bool IsGyro => flags.HasFlag(Flags.Gyro);
        public bool IsWeapon => flags.HasFlag(Flags.Weapon);
        public bool IsMelee => flags.HasFlag(Flags.Melee);
        public bool IsShortRanged => flags.HasFlag(Flags.Short);
        public bool HasWheels => flags.HasFlag(Flags.Wheels);
        public bool IsAIModule => attributesHash.HasFlagBitShift(BlockAttributes.AI);
        public bool IsPlayerControlled => attributesHash.HasFlagBitShift(BlockAttributes.PlayerCab);
        public bool IsCab => IsAIModule || IsPlayerControlled;
        public bool FloatsOnWater => flags.HasFlag(Flags.Floats);
        public bool AttachesAndOrDetachesBlocks => flags.HasFlag(Flags.ControlsBlockman);
        public bool HasCircuits => attributesHash.HasFlagBitShift(BlockAttributes.CircuitsEnabled);
        public bool RequiresAnchoring => attributesHash.HasFlagBitShift(BlockAttributes.Anchored);
        public bool IsRotatingAnchor => attributesHash.HasFlagBitShift(BlockAttributes.AnchoredMobile);
        public bool UsesChunks => attributesHash.HasFlagBitShift(BlockAttributes.ResourceBased);
        public bool IsAutominer => attributesHash.HasFlagBitShift(BlockAttributes.Mining);
        public bool IsGeothermal => attributesHash.HasFlagBitShift(BlockAttributes.Steam);
        public bool IsGenerator => attributesHash.HasFlagBitShift(BlockAttributes.PowerProducer);
        public bool IsBattery => attributesHash.HasFlagBitShift(BlockAttributes.PowerStorage);
        public bool UsesPower => attributesHash.HasFlagBitShift(BlockAttributes.PowerConsumer);
        public bool IsFuelTank => attributesHash.HasFlagBitShift(BlockAttributes.FuelStorage);
        public bool UsesFuel => attributesHash.HasFlagBitShift(BlockAttributes.FuelConsumer);
        public bool IsSCU => attributesHash.HasFlagBitShift(BlockAttributes.BlockStorage);
    }
}
