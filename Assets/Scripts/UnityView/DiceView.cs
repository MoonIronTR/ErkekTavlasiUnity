using System.Collections.Generic;
using UnityEngine;

namespace ErkekTavlasi.UnityView
{
    public class DiceView : MonoBehaviour
    {
        private readonly List<GameObject> pips = new List<GameObject>();
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Initialize(Material diceMaterial, Material pipMaterial)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Die Body";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * 0.46f;
            cube.GetComponent<Renderer>().sharedMaterial = diceMaterial;
            DestroyImmediate(cube.GetComponent<Collider>());

            body = gameObject.AddComponent<Rigidbody>();
            body.mass = 0.25f;
            body.linearDamping = 0.22f;
            body.angularDamping = 0.16f;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.interpolation = RigidbodyInterpolation.Interpolate;

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.48f;
            collider.material = new PhysicsMaterial("Dice Surface")
            {
                bounciness = 0.42f,
                dynamicFriction = 0.30f,
                staticFriction = 0.42f,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average
            };

            CreateFace(1, Vector3.up, Vector3.right, Vector3.forward, pipMaterial);
            CreateFace(6, Vector3.down, Vector3.right, Vector3.back, pipMaterial);
            CreateFace(2, Vector3.right, Vector3.forward, Vector3.up, pipMaterial);
            CreateFace(5, Vector3.left, Vector3.back, Vector3.up, pipMaterial);
            CreateFace(3, Vector3.forward, Vector3.right, Vector3.up, pipMaterial);
            CreateFace(4, Vector3.back, Vector3.left, Vector3.up, pipMaterial);
        }

        public void Throw(Vector3 position, Vector3 force, Vector3 torque)
        {
            EnsureBody();
            transform.position = position;
            transform.rotation = Random.rotation;
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.AddForce(force, ForceMode.Impulse);
            body.AddTorque(torque, ForceMode.Impulse);
        }

        public void RollFromCurrentPose(Vector3 velocityChange, Vector3 angularVelocityChange)
        {
            EnsureBody();
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
            body.AddForce(velocityChange, ForceMode.VelocityChange);
            body.AddTorque(angularVelocityChange, ForceMode.VelocityChange);
        }

        public void SetKinematicPose(Vector3 position, Quaternion rotation)
        {
            EnsureBody();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            transform.position = position;
            transform.rotation = rotation;
        }

        public void Park(Vector3 position, int value)
        {
            EnsureBody();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            transform.position = position;
            float yaw = Random.Range(0, 4) * 90f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f) * RotationForTopValue(value);
            body.isKinematic = true;
        }

        public bool IsSettled(float maxLinearSpeed = 0.04f, float maxAngularSpeed = 0.18f)
        {
            EnsureBody();
            if (body == null || body.isKinematic)
            {
                return true;
            }

            return body.IsSleeping()
                || (body.linearVelocity.sqrMagnitude <= maxLinearSpeed * maxLinearSpeed
                    && body.angularVelocity.sqrMagnitude <= maxAngularSpeed * maxAngularSpeed);
        }

        /// <summary>
        /// Returns true when the die's rigidbody has come to rest.
        /// </summary>
        public bool IsSleeping()
        {
            EnsureBody();
            return body != null && body.IsSleeping();
        }

        /// <summary>
        /// Reads which face value is currently on top by checking
        /// which local face normal is most aligned with world-up.
        /// </summary>
        public int ReadTopFace()
        {
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            int[] values = { 1, 6, 2, 5, 3, 4 };
            float bestDot = -1f;
            int result = 1;
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 wn = transform.TransformDirection(normals[i]);
                float d = Vector3.Dot(wn, Vector3.up);
                if (d > bestDot) { bestDot = d; result = values[i]; }
            }
            return result;
        }

        /// <summary>
        /// Returns true if no face is closely aligned with world-up,
        /// meaning the die is leaning / crooked (kirik).
        /// </summary>
        public bool IsCrooked(float threshold = 0.82f)
        {
            Vector3[] normals = { Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back };
            float bestDot = -1f;
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 wn = transform.TransformDirection(normals[i]);
                float d = Vector3.Dot(wn, Vector3.up);
                if (d > bestDot) bestDot = d;
            }
            return bestDot < threshold;
        }

        /// <summary>
        /// Stops all motion and locks the die in its current pose.
        /// </summary>
        public void Freeze()
        {
            EnsureBody();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
        }

        private void EnsureBody()
        {
            if (body == null)
            {
                body = GetComponent<Rigidbody>();
            }
        }

        private Quaternion RotationForTopValue(int value)
        {
            if (value == 1) return Quaternion.identity;
            if (value == 2) return Quaternion.Euler(0, 0, 90);
            if (value == 3) return Quaternion.Euler(-90, 0, 0);
            if (value == 4) return Quaternion.Euler(90, 0, 0);
            if (value == 5) return Quaternion.Euler(0, 0, -90);
            return Quaternion.Euler(180, 0, 0);
        }

        private void CreateFace(int value, Vector3 normal, Vector3 right, Vector3 up, Material pipMaterial)
        {
            foreach (Vector2 spot in Spots(value))
            {
                GameObject pip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pip.name = "Pip";
                pip.transform.SetParent(transform, false);
                pip.transform.localScale = new Vector3(0.065f, 0.010f, 0.065f);
                pip.transform.localPosition = normal * 0.236f + right * spot.x * 0.126f + up * spot.y * 0.126f;
                pip.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
                pip.GetComponent<Renderer>().sharedMaterial = pipMaterial;
                DestroyImmediate(pip.GetComponent<Collider>());
                pips.Add(pip);
            }
        }

        private List<Vector2> Spots(int value)
        {
            List<Vector2> spots = new List<Vector2>();
            if (value == 1) spots.Add(Vector2.zero);
            if (value == 2) { spots.Add(new Vector2(-1, -1)); spots.Add(new Vector2(1, 1)); }
            if (value == 3) { spots.Add(new Vector2(-1, -1)); spots.Add(Vector2.zero); spots.Add(new Vector2(1, 1)); }
            if (value == 4) { spots.Add(new Vector2(-1, -1)); spots.Add(new Vector2(1, -1)); spots.Add(new Vector2(-1, 1)); spots.Add(new Vector2(1, 1)); }
            if (value == 5) { spots.Add(new Vector2(-1, -1)); spots.Add(new Vector2(1, -1)); spots.Add(Vector2.zero); spots.Add(new Vector2(-1, 1)); spots.Add(new Vector2(1, 1)); }
            if (value == 6) { spots.Add(new Vector2(-1, -1)); spots.Add(new Vector2(1, -1)); spots.Add(new Vector2(-1, 0)); spots.Add(new Vector2(1, 0)); spots.Add(new Vector2(-1, 1)); spots.Add(new Vector2(1, 1)); }
            return spots;
        }
    }
}
