using System;

namespace TerraTechETCUtil
{
    /// <summary>
    /// A quick lookup for block data rather than manually searching through every block for it.
    /// </summary>
    public struct BlockDetails
    {
        /// <summary>
        /// Special flags for block data
        /// </summary>
        public enum Flags
        {
            /// <summary>
            /// Nothing
            /// </summary>
            None = 0,
            /// <summary> Has <see cref="BoosterJet"/> </summary>
            Boosters = 1,
            /// <summary> Has <see cref="FanJet"/></summary>
            Fans = 2,
            /// <summary> Has <see cref="ModuleWing"/></summary>
            Wings = 4,
            /// <summary> Has <see cref="ModuleHover"/></summary>
            Hovers = 8,
            /// <summary> Has <see cref="ModuleWheels"/></summary>
            Wheels = 16,
            /// <summary> Has <see cref="ModuleGyro"/></summary>
            Gyro = 32,
            /// <summary> Has <see cref="ModuleAntiGravityEngine"/></summary>
            AntiGrav = 64,
            /// <summary> Has ModuleOmniEngine</summary>
            OmniDirectional = 128,
            /// <summary> Has <see cref="ModuleWeapon"/></summary>
            Weapon = 256,
            /// <summary> Has <see cref="ModuleMeleeWeapon"/></summary>
            Melee = 512,
            /// <summary> Has short-ranged <see cref="ModuleWeapon"/></summary>
            Short = 1024,
            /// <summary> Has <see cref="ModuleShieldGenerator"/></summary>
            Bubble = 2048,
            /// <summary> Has lower density than default settings water in <b>WaterMod</b></summary>
            WaterFloats = 4096,
            /// <summary> Can attach or detach other blocks </summary>
            ControlsBlockman = 8192,
            /// <summary> Has <see cref="ModuleBalloon"/></summary>
            AirFloats = 16384,
            /// <summary> unknown unset </summary>
            UNUSED_unset = 32768,
        }

        /// <summary>
        /// Flags assigned to this block
        /// </summary>
        public Flags flags;
        /// <summary>
        /// Block attaibutes assigned to this block
        /// </summary>
        public BlockAttributes attributesHash;

        /// <summary>
        /// Get the <see cref="BlockDetails"/> for the block.  
        /// <para>Will not be fully available until all blocks are loaded in</para>
        /// </summary>
        /// <param name="BT"></param>
        public BlockDetails(BlockTypes BT)
        {
            flags = default;
            attributesHash = default;
            BlockIndexer.GetBlockDetails_Internal(BT, ref this);
        }
        /// <summary>
        /// Block has no abilities.  Standard building block
        /// </summary>
        public bool IsBasic => flags == 0 && attributesHash == 0;
        private const Flags MovementFlags = Flags.Boosters | Flags.Fans | 
            Flags.Wings | Flags.Hovers |  Flags.Wheels | Flags.Gyro | 
            Flags.AntiGrav | Flags.OmniDirectional | Flags.AirFloats;
        /// <summary>
        /// Can move the Tech in some controllable, sustainable way
        /// </summary>
        public bool DoesMovement => flags.HasAnyFlag(MovementFlags);
        private const Flags RotateFlags = Flags.Boosters |
            Flags.Fans | Flags.Wings | Flags.Hovers |
            Flags.Wheels | Flags.Gyro | Flags.OmniDirectional;
        /// <summary>
        /// Can rotate the Tech in some controllable, sustainable way
        /// </summary>
        public bool CanRotateTech => flags.HasAnyFlag(RotateFlags);
        private const Flags PushFlags = Flags.Boosters |
            Flags.Fans | Flags.Hovers | Flags.Wheels | Flags.OmniDirectional;
        /// <summary>
        /// Can move the Tech off the ground in some controllable, sustainable way
        /// </summary>
        public bool CanPushTech => flags.HasAnyFlag(PushFlags);
        private const Flags FloatControlFlags = Flags.Boosters | Flags.Fans | Flags.Wings | 
            Flags.Hovers | Flags.OmniDirectional | Flags.WaterFloats | Flags.AirFloats;
        /// <summary>
        /// Can float the Tech off the ground in some controllable, sustainable way
        /// </summary>
        public bool CanWorkOffGround => flags.HasAnyFlag(FloatControlFlags);

        /// <summary> Has <see cref="ModuleBalloon"/></summary>
        public bool HasFloaters => flags.HasFlag(Flags.AirFloats);
        /// <summary> Has <see cref="BoosterJet"/> </summary>
        public bool HasBoosters => flags.HasFlag(Flags.Boosters);
        /// <summary> Has <see cref="FanJet"/></summary>
        public bool HasFans => flags.HasFlag(Flags.Fans);
        /// <summary> Has <see cref="ModuleWing"/></summary>
        public bool HasWings => flags.HasFlag(Flags.Wings);
        /// <summary> Has <see cref="ModuleHover"/></summary>
        public bool HasHovers => flags.HasFlag(Flags.Hovers);
        /// <summary> Has <see cref="ModuleAntiGravityEngine"/></summary>
        public bool HasAntiGravity => flags.HasFlag(Flags.AntiGrav);
        /// <summary> Has ModuleOmniEngine</summary>
        public bool IsOmniDirectional => flags.HasFlag(Flags.OmniDirectional);
        /// <summary> Has <see cref="ModuleShieldGenerator"/></summary>
        public bool IsBubble => flags.HasFlag(Flags.Bubble);
        /// <summary> Has <see cref="ModuleGyro"/></summary>
        public bool IsGyro => flags.HasFlag(Flags.Gyro);
        /// <summary> Has <see cref="ModuleWeapon"/></summary>
        public bool IsWeapon => flags.HasFlag(Flags.Weapon);
        /// <summary> Has <see cref="ModuleMeleeWeapon"/></summary>
        public bool IsMelee => flags.HasFlag(Flags.Melee);
        /// <summary> Has short-ranged <see cref="ModuleWeapon"/></summary>
        public bool IsShortRanged => flags.HasFlag(Flags.Short);
        /// <summary> Has <see cref="ModuleWheels"/></summary>
        public bool HasWheels => flags.HasFlag(Flags.Wheels);
        /// <summary> Has <see cref="ModuleAIBot"/> with <see cref="TechAI.AITypes.Guard"/> or 
        /// <see cref="TechAI.AITypes.Escort"/> enabled</summary>
        public bool IsAIModule => attributesHash.HasFlagBitShift(BlockAttributes.AI);
        /// <summary> Has <see cref="ModuleTechController"/> with <see cref="ModuleTechController.m_PlayerInput"/> true</summary>
        public bool IsPlayerControlled => attributesHash.HasFlagBitShift(BlockAttributes.PlayerCab);
        /// <summary> Has <see cref="ModuleTechController"/> </summary>
        public bool IsCab => IsAIModule || IsPlayerControlled;
        /// <summary> Has lower density than default settings water in <b>WaterMod</b></summary>
        public bool FloatsOnWater => flags.HasFlag(Flags.WaterFloats);
        /// <summary> Can attach or detach other blocks </summary>
        public bool AttachesAndOrDetachesBlocks => flags.HasFlag(Flags.ControlsBlockman);
        /// <summary> Can send and recieve <see cref="Circuits"/> signals </summary>
        public bool HasCircuits => attributesHash.HasFlagBitShift(BlockAttributes.CircuitsEnabled);
        /// <summary> Needs to be anchored to function </summary>
        public bool RequiresAnchoring => attributesHash.HasFlagBitShift(BlockAttributes.Anchored);
        /// <summary> Is a rotating anchor </summary>
        public bool IsRotatingAnchor => attributesHash.HasFlagBitShift(BlockAttributes.AnchoredMobile);
        /// <summary> Uses <see cref="ResourcePickup"/>s aka Chunks </summary>
        public bool UsesChunks => attributesHash.HasFlagBitShift(BlockAttributes.ResourceBased);
        /// <summary> Has <see cref="ModuleItemProducer"/> </summary>
        public bool IsAutominer => attributesHash.HasFlagBitShift(BlockAttributes.Mining);
        /// <summary> Has <see cref="ModuleEnergy"/> with <see cref="ModuleEnergy.OutputConditionFlags.Thermal"/> and 
        /// positive energy generation</summary>
        public bool IsGeothermal => attributesHash.HasFlagBitShift(BlockAttributes.Steam);
        /// <summary> Has <see cref="ModuleEnergy"/> with positive energy generation </summary>
        public bool IsGenerator => attributesHash.HasFlagBitShift(BlockAttributes.PowerProducer);
        /// <summary> Has <see cref="ModuleEnergyStore"/> </summary>
        public bool IsBattery => attributesHash.HasFlagBitShift(BlockAttributes.PowerStorage);
        /// <summary> Has <see cref="ModuleEnergy"/> with anything that consumes energy </summary>
        public bool UsesPower => attributesHash.HasFlagBitShift(BlockAttributes.PowerConsumer);
        /// <summary> Has <see cref="ModuleFuelTank"/> </summary>
        public bool IsFuelTank => attributesHash.HasFlagBitShift(BlockAttributes.FuelStorage);
        /// <summary> Has <see cref="ModuleBooster"/> with fuel drain and other similar fuel burners </summary>
        public bool UsesFuel => attributesHash.HasFlagBitShift(BlockAttributes.FuelConsumer);
        /// <summary> Has <see cref="ModuleHeart"/> </summary>
        public bool IsSCU => attributesHash.HasFlagBitShift(BlockAttributes.BlockStorage);
    }
}
