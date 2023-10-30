using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TerraTechETCUtil
{
    internal class PhysicsLockOther : MonoBehaviour, IWorldTreadmill
    {
        private static PhysicsLockOther prefab;
        private const string nameDefault = "PhysicsLockExt_";

        internal Rigidbody rbody;
        internal Transform trans => rbody.transform;
        internal Vector3 offset = Vector3.zero;
        internal Vector3 offsetCalc => trans.InverseTransformPoint(targetTrans.TransformPoint(offset));
        internal Quaternion offsetQuat = Quaternion.identity;
        internal Transform targetTrans = null;

        // FIXED WORLD
        internal Tank tank = null;
        private TankBlock block;
        private Visible visible;
        private bool fixedGround = false;

        public static void FirstInit()
        {
            if (prefab != null)
                return;
            var GO = new GameObject(nameDefault);
            GO.layer = Globals.inst.layerCosmetic;
            prefab = GO.AddComponent<PhysicsLockOther>();
            GO.AddComponent<MeshFilter>();
            GO.AddComponent<MeshRenderer>();
            var SC = GO.AddComponent<SphereCollider>();
            SC.radius = 0.2f;
            SC.material = new PhysicMaterial();
            var rb = GO.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.None;
            GO.SetActive(false);
            prefab.CreatePool(1);
        }
        public static PhysicsLockOther InitFreestanding(Transform rootTrans, Vector3 contactPoint)
        {
            FirstInit();
            var MTA = prefab.Spawn();
            MTA.fixedGround = true;
            return MTA.InitFreestanding_Internal(rootTrans, contactPoint);
        }
        private PhysicsLockOther InitFreestanding_Internal(Transform rootTrans, Vector3 contactPointWorld)
        {
            rbody = GetComponent<Rigidbody>();
            transform.position = contactPointWorld;
            transform.rotation = rootTrans.rotation;
            rbody.freezeRotation = true;
            rbody.mass = 9001;
            rbody.constraints = RigidbodyConstraints.FreezeAll;
            ManWorldTreadmill.inst.AddListener(this);
            return this;
        }
        private void DeInitFreestanding()
        {
            ManWorldTreadmill.inst.RemoveListener(this);
            this.Recycle();
        }

        public void OnMoveWorldOrigin(IntVector3 move)
        {
            transform.position += move;
        }
        public void OnRecycle() {}

        // DYNAMIC TECH
        private void OnBlockTableChanged()
        {
            main.OnPositionsAltered();
        }

        // GENERAL
        private PhysicsLock coupler;
        private Transform couplerVisual;

        private PhysicsLockMain main => coupler.main;
        private void InitStart(Transform targTrans, Vector3 scenePos, PhysicsLock couplerSet)
        {
            coupler = couplerSet;
            try
            {
                /*
                GetComponent<MeshFilter>().sharedMesh = main.coupler.visualPrefab.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                GetComponent<MeshRenderer>().sharedMaterial = main.coupler.visualPrefab.GetComponentInChildren<MeshRenderer>(true).sharedMaterial;
                */
                if (!couplerVisual && coupler.visualPrefab)
                {
                    couplerVisual = Instantiate(coupler.visualPrefab, null).transform;
                }
            }
            catch { }
            targetTrans = targTrans;
            offset = targetTrans.InverseTransformPoint(scenePos);
            offsetQuat = Quaternion.Inverse(rbody.transform.rotation) * main.headMount.rotation;
        }
        private static PhysicsLockOther InitFixedFreestanding(PhysicsLock couplerInst, WorldPosition fixedWorldPosition, Collider collider)
        {
            PhysicsLockMain mainSide = couplerInst.main;
            PhysicsLockOther other = mainSide.other;
            if (other == null)
                other = InitFreestanding(mainSide.transform.root, fixedWorldPosition.ScenePosition);
            other.InitStart(collider.transform, fixedWorldPosition.ScenePosition, couplerInst);
            return other;
        }
        private void InitLooseTankBlock(PhysicsLock couplerInst, Visible vis, TankBlock TB, Rigidbody rbody)
        {
            if (vis == null)
                throw new NullReferenceException("InitLooseTankBlock expected to be targeted at TankBlock but given Collider is not part of a TankBlock.visible");
            if (TB == null)
                throw new NullReferenceException("InitLooseTankBlock expected to be targeted at TankBlock but given Collider is not part of a TankBlock");
            if (rbody == null)
                throw new NullReferenceException("InitLooseTankBlock expected to be targeted at a tank with a working Rigidbody, but the tank has no rigidbody");
            PhysicsLockMain mainSide = couplerInst.main;

            block = TB;
            block.DetachingEvent.Subscribe(mainSide.OnHierachyCompromised);
            visible = vis;
            vis.RecycledEvent.Subscribe(mainSide.OnHierachyCompromised);
        }
        private void InitAttachedTankBlock(PhysicsLock couplerInst, Visible vis, Tank tank, TankBlock TB, Rigidbody rbody)
        {
            if (vis == null)
                throw new NullReferenceException("InitAttachedTankBlock expected to be targeted at TankBlock but given Collider is not part of a TankBlock.visible");
            if (TB == null)
                throw new NullReferenceException("InitAttachedTankBlock expected to be targeted at TankBlock but given Collider is not part of a TankBlock");
            if (tank == null)
                throw new NullReferenceException("InitAttachedTankBlock expected to be targeted at an attached TankBlock, but the block is not part of any Tank");
            if (rbody == null)
                throw new NullReferenceException("InitAttachedTankBlock expected to be targeted at a tank with a working Rigidbody, but the tank has no rigidbody");
            PhysicsLockMain mainSide = couplerInst.main;

            this.tank = tank;
            tank.blockman.BlockTableRecentreEvent.Subscribe(OnBlockTableChanged);
            tank.TankRecycledEvent.Subscribe(mainSide.OnHierachyCompromised);
            tank.AnchorEvent.Subscribe(mainSide.OnHierachyChangedState);
        }

        internal static PhysicsLockOther InitCollider(PhysicsLock couplerInst, WorldPosition worldPosition, Collider hitCollider)
        {
            Visible vis = ManVisible.inst.FindVisible(hitCollider);
            if (vis == null)
                return InitFixedFreestanding(couplerInst, worldPosition, hitCollider);
            TankBlock TB = vis.block;
            if (TB == null)
                return InitFixedFreestanding(couplerInst, worldPosition, hitCollider);
            Rigidbody rbody = hitCollider.transform.root.GetComponent<Rigidbody>();
            if (rbody == null)
                return InitFixedFreestanding(couplerInst, worldPosition, hitCollider);
            PhysicsLockOther other = rbody.GetComponent<PhysicsLockOther>();
            if (other == null)
                other = rbody.gameObject.AddComponent<PhysicsLockOther>();
            other.rbody = rbody;
            other.InitStart(hitCollider.transform, worldPosition.ScenePosition, couplerInst);
            Tank tank = TB.tank;
            if (tank != null)
                other.InitLooseTankBlock(couplerInst, vis, TB, rbody);
            other.InitAttachedTankBlock(couplerInst, vis, tank, TB, rbody);
            return other;
        }

        private void Update()
        {
            if (couplerVisual)
            {
                couplerVisual.position = rbody.transform.TransformPoint(offset);
                couplerVisual.rotation = rbody.transform.rotation * offsetQuat;
            }
        }

        internal void DetachAndRecycle()
        {
            if (tank)
            {
                tank.blockman.BlockTableRecentreEvent.Unsubscribe(OnBlockTableChanged);
                tank.TankRecycledEvent.Unsubscribe(main.OnHierachyCompromised);
            }
            if (block)
            {
                block.DetachingEvent.Unsubscribe(main.OnHierachyCompromised);
            }
            if (visible)
            {
                visible.RecycledEvent.Unsubscribe(main.OnHierachyCompromised);
            }
            if (couplerVisual)
            {
                Destroy(couplerVisual);
                couplerVisual = null;
            }
            if (fixedGround)
                DeInitFreestanding();
            else
            {
                Destroy(this);
            }
        }
    }
    /// <summary>
    /// The main side of the PhysicsLock which holds the module. Not to be called externally.
    /// <para>MUST BE ON TANK SIDE!</para>
    ///     Rests in the root layer of Transform hierachy to function.
    /// </summary>
    internal class PhysicsLockMain : MonoBehaviour
    {
        private Tank tank;
        internal ConfigurableJoint joint;
        protected const float IgnoreImpulseBelow = 6;

        internal PhysicsLock coupler;
        internal PhysicsLockOther other;
        internal Transform headMount => lockTrans;

        private Transform lockTrans;
        private Vector3 offset = Vector3.zero;
        private Vector3 offsetCalc => tank.trans.InverseTransformPoint(lockTrans.TransformPoint(offset));
        private HashSet<Collider> physLockCol = new HashSet<Collider>();
        internal static PhysicsLockMain InitStart(Tank host, Transform lockTransform, Vector3 scenePos)
        {
            PhysicsLockMain phyLock = host.GetComponent<PhysicsLockMain>();
            if (phyLock == null)
            {
                phyLock = host.gameObject.AddComponent<PhysicsLockMain>();
                phyLock.tank = host;
                phyLock.lockTrans = lockTransform;

                phyLock.joint = null;
                phyLock.coupler = null;
                phyLock.other = null;
            }
            phyLock.offset = phyLock.lockTrans.InverseTransformPoint(scenePos);
            return phyLock;
        }

        internal void OnPositionsAltered()
        {
            if (joint != null)
            {
                joint.anchor = offsetCalc;
                joint.connectedAnchor = other.offsetCalc;
            }
        }
        internal void InitSetup(PhysicsLockOther target, PhysicsLock info)
        {
            if (joint == null)
            {
                joint = tank.gameObject.AddComponent<ConfigurableJoint>();
                tank.blockman.BlockTableRecentreEvent.Subscribe(OnPositionsAltered);
                tank.CollisionEvent.Subscribe(OnCollision);
            }
            if (target == null)
                throw new NullReferenceException("InitSetup expects a valid target but it was null!");
            coupler = info;
            other = target;
            joint.connectedBody = target.rbody;
            joint.autoConfigureConnectedAnchor = false;
            joint.configuredInWorldSpace = false;
            joint.enablePreprocessing = false;
            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
            joint.enableCollision = info.selfCollision;

            joint.massScale = 1;
            joint.axis = info.axis;
            joint.secondaryAxis = info.axis2;

            /*
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionAngle = 0.75f;
            joint.projectionDistance = 0.1f;
            */

            OnPositionsAltered();

            /*
            joint.xDrive = info.zPiston;
            joint.yDrive = info.zPiston;
            joint.zDrive = info.zPiston;

            joint.angularXDrive = info.zRotor;
            joint.angularYZDrive = info.zRotor;
            */

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = info.zPiston.maximumForce == 0 ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = info.zRotor.maximumForce == 0 ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;

            joint.linearLimit = info.posLimit;
            joint.linearLimitSpring = info.posSpring;
            joint.breakForce = info.PistonBreak;
            joint.breakTorque = info.RotorBreak;

            info.extent = info.offsetSpacer;
        }

        public static bool CanRunPhysics(Tank tank)
        {
            if (!ManNetwork.IsNetworked || tank.netTech == null)
                return true;
            return tank.netTech.hasAuthority;
        }
        public static bool WithinBox(Vector3 vec, float extents)
        {
            return vec.x >= -extents && vec.x <= extents && vec.y >= -extents && vec.y <= extents && vec.z >= -extents && vec.z <= extents;
        }
        public void OnCollision(Tank.CollisionInfo collide, Tank.CollisionInfo.Event whack)
        {
            try
            {
                if (whack == Tank.CollisionInfo.Event.NonAttached)
                    return;
                Tank.CollisionInfo.Obj thisC;
                Tank.CollisionInfo.Obj otherObj;
                if (collide.a.tank == tank)
                {
                    thisC = collide.a;
                    otherObj = collide.b;
                }
                else
                {
                    otherObj = collide.a;
                    thisC = collide.b;
                }
                if (!physLockCol.Contains(thisC.collider))
                    return;
                if (otherObj.tank && other.tank && otherObj.tank == other.tank && WithinBox(collide.impulse, IgnoreImpulseBelow))
                {
                    Vector3 impulse;
                    if (Vector3.Dot(collide.normal, collide.impulse) >= 0)
                        impulse = -collide.impulse;
                    else
                        impulse = collide.impulse;
                    if (CanRunPhysics(tank))
                        tank.rbody.AddForceAtPosition(-impulse, collide.point, ForceMode.Force);
                    if (CanRunPhysics(other.tank))
                        other.tank.rbody.AddForceAtPosition(impulse, collide.point, ForceMode.Force);
                }
            }
            catch (Exception e)
            {
                Debug_TTExt.Log("Whoops - PhysicsCoupler.OnCollision " + e);
            }
        }
        private void FixedUpdate()
        {
            
        }

        internal void DetachAndRecycle()
        {
            tank.CollisionEvent.Unsubscribe(OnCollision);
            tank.blockman.BlockTableRecentreEvent.Unsubscribe(OnPositionsAltered);
            if (joint)
            {
                Destroy(joint);
                joint = null;
            }
            Destroy(this);
        }
        public void OnJointBreak(float force)
        {
            OnHierachyCompromised();
        }
        internal void OnHierachyCompromised()
        {
            PhysicsLock cache = coupler;
            cache.Detach();
        }
        internal void OnHierachyCompromised(Tank unused)
        {
            OnHierachyCompromised();
        }
        internal void OnHierachyCompromised(Visible unused)
        {
            OnHierachyCompromised();
        }

        internal void OnHierachyChangedState()
        {
            PhysicsLock cache = coupler;
            if (cache == null) throw new NullReferenceException("OnHierachyChangedState called but coupler was null");
            cache.TryReAttach();
        }
        internal void OnHierachyChangedState(ModuleAnchor _, bool _1, bool _2)
        {
            OnHierachyChangedState();
        }
    }

    /// <summary>
    /// This provides a simple class to connect two Rigidbodies together in a rough, but understandable way.
    ///   Does not support breaking forces of infinity since that will cause unstability
    /// </summary>
    public class PhysicsLock
    {
        private Collider col1;
        private Collider col2;

        private ConfigurableJoint joint => main.joint;

        internal PhysicsLockMain main { get; private set; } = null;
        internal PhysicsLockOther other { get; private set; } = null;

        public bool IsAttached => main;
        public GameObject visualPrefab;
        public float AddAll(Vector3 input)
        {
            return input.x + input.y + input.z;
        }

        public Vector3 UpAxis => main ? main.headMount.up : throw new InvalidOperationException("UpAxis was called with no main assigned");
        public Vector3 FwdAxis => main ? main.headMount.forward : throw new InvalidOperationException("FwdAxis was called with no main assigned");


        /// <summary>
        /// Extends the head of the joint along the y-axis.  Useful for pistons
        /// </summary>
        public float extent
        {
            get
            {
                if (joint)
                    return AddAll(Vector3.Scale(joint.targetPosition, FwdAxis));
                return 0;
            }
            set
            {
                if (joint)
                    joint.targetPosition = FwdAxis * value;
            }
        }
        /// <summary>
        /// Rotates the coupler around the y-axis constantly.  Useful for motors
        /// </summary>
        public float rotation {
            get
            {
                if (joint)
                    return AddAll(Vector3.Scale(joint.targetAngularVelocity, FwdAxis));
                return 0;
            }
            set
            {
                if (joint)
                    joint.targetAngularVelocity = FwdAxis * value;
            }
        }
        /// <summary>
        /// Sets the y-angle of the coupler to something precise.  Useful for servos
        /// </summary>
        public float angle
        {
            get
            {
                if (joint)
                    return Vector3.SignedAngle(joint.targetRotation * UpAxis, UpAxis, FwdAxis);
                return 0;
            }
            set
            {
                if (joint)
                    joint.targetRotation = Quaternion.AngleAxis(value, FwdAxis);
            }
        }

        public Event<Transform> AttachedEvent = new Event<Transform>();
        public EventNoParams DetachedEvent = new EventNoParams();


        //internal EventNoParams updatedEventHook;
        //internal EventNoParams detachedEventHook;
        public bool selfCollision = true;
        public float offsetSpacer = 0.5f;
        public SoftJointLimit posLimit;
        public SoftJointLimitSpring posSpring;
        public Vector3 axis = Vector3.forward;
        public Vector3 axis2 = Vector3.up;

        public JointDrive zPiston;
        public float PistonBreak = 10000;

        public JointDrive zRotor;
        public float RotorBreak = 10000;

        public PhysicsLock()
        {
            var defaults = new JointDrive()
            {
                maximumForce = 0,
                positionDamper = 50,
                positionSpring = 500
            };
            zPiston = defaults;
            zRotor = defaults;
            posLimit = new SoftJointLimit()
            {
                bounciness = 0.2f,
                contactDistance = 0.05f,
                limit = 360,
            };
            posSpring = new SoftJointLimitSpring()
            {
                damper = 5000,
                spring = 5000,
            };
        }

        public void Attach(WorldPosition worldPos, Collider col, WorldPosition worldPosOther, Collider colOther)
        {
            if (col == null || col.transform == null)
                throw new NullReferenceException("Attach expects a valid collider with transform but it was null!");
            if (colOther == null || colOther.transform == null)
                throw new NullReferenceException("Attach expects a valid collider2 with transform but it was null!");
            Transform rootTrans = col.transform.root;
            Transform rootTransOther = colOther.transform.root;
            try
            {
                if (rootTrans && rootTrans.GetComponent<Rigidbody>() != null &&
                    rootTrans.GetComponent<Tank>() != null && rootTrans.root == rootTrans)
                {
                    main = PhysicsLockMain.InitStart(rootTrans.GetComponent<Tank>(), col.transform, worldPos.ScenePosition);
                    if (!main) throw new NullReferenceException("main is null for some reason");
                    other = PhysicsLockOther.InitCollider(this, worldPosOther, colOther);
                    if (!other) throw new NullReferenceException("other is null for some reason");
                    main.InitSetup(other, this);
                    col1 = col;
                    col2 = colOther;
                }
                else if (rootTransOther && rootTransOther.GetComponent<Rigidbody>() != null &&
                    rootTransOther.GetComponent<Tank>() != null && rootTransOther.root == rootTransOther)
                {
                    main = PhysicsLockMain.InitStart(rootTransOther.GetComponent<Tank>(), colOther.transform, worldPosOther.ScenePosition);
                    if (!main) throw new NullReferenceException("main is null for some reason");
                    other = PhysicsLockOther.InitCollider(this, worldPos, col);
                    if (!other) throw new NullReferenceException("other is null for some reason");
                    main.InitSetup(other, this);
                    col1 = colOther;
                    col2 = col;
                }
                else
                {
                    throw new InvalidOperationException("PhysicsLock cannot be initiated for two Transforms with no rigidbodies");
                }
                AttachedEvent.Send(other.targetTrans);
            }
            catch (Exception e)
            {
                throw new Exception("PhysicsLock failed on Attach() - " + e);
            }
        }
        public static bool inProgress = false;
        public void Detach()
        {
            int level = 0;
            try
            {
                if (inProgress) throw new InvalidOperationException("cannot nest PhysicsLock.Detach() commands!");
                inProgress = true;
                if ((bool)other != (bool)main) throw new NullReferenceException("main & other: one of which exists but both should exist or not exist");
                DetachedEvent.Send();
                level++;
                other.DetachAndRecycle();
                other = null;
                level++;
                main.DetachAndRecycle();
                main = null;
                level++;
            }
            catch (Exception e)
            {
                inProgress = false;
                throw new Exception("PhysicsLock failed on Detach(), level [" + level + "] - " + e);
            }
            inProgress = false;
        }
        public void TryReAttach()
        {
            int error = 0;
            try
            {
                if (main && other && joint)
                {
                    error++;
                    WorldPosition pos1World = WorldPosition.FromScenePosition(joint.transform.TransformPoint(joint.anchor));
                    error++;
                    WorldPosition pos2World = WorldPosition.FromScenePosition(joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
                    error++;
                    Detach();
                    error++;
                    Attach(pos1World, col1, pos2World, col2);
                    error++;
                }
            }
            catch (Exception e)
            {
                throw new Exception("ReAttach [" + error + "] - " + e);
            }
        }
    }
}
