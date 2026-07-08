using System;
using UnityEngine;

namespace MoSimLib
{
    /// <summary>
    /// A custom float class that is obfuscated in memory to deter tampering
    /// with memory scanning tools like Cheat Engine.
    /// </summary>
    /// <remarks>
    /// This class stores the float's bit representation XORed with a random key.
    /// The key changes every time the value is set, making the in-memory value
    /// appear as a random, meaningless integer. This prevents simple searches
    /// for a specific float value.
    ///
    /// While this provides a strong deterrent, it is not a 100% foolproof solution
    /// against dedicated reverse engineers. It's best used as part of a layered
    /// security approach.
    /// </remarks>
    [Serializable]
    public struct SecureFloat
    {
        // The obfuscation key changes every time the value is set.
        // The [SerializeField] attribute makes this field visible to the Unity
        // serialization system, allowing the PropertyDrawer to access it.
        public int _obfuscationKey;

        // The in-memory value is the float's bit representation XORed with the key.
        // The [SerializeField] attribute is necessary for the PropertyDrawer.
        public int _encryptedValue;

        /// <summary>
        /// Gets or sets the value of the secure float.
        /// </summary>
        public float Value
        {
            get
            {
                // Reverse the obfuscation: XOR the encrypted value with the key.
                int decryptedBits = _encryptedValue ^ _obfuscationKey;

                // Convert the integer bit representation back to a float.
                return BitConverter.Int32BitsToSingle(decryptedBits);
            }
            set
            {
                // Create a new random obfuscation key.
                // Using a simple XOR with a random integer is effective against
                // basic memory scanning.
                _obfuscationKey = new System.Random().Next();

                // Convert the float value to its integer bit representation.
                int floatBits = BitConverter.SingleToInt32Bits(value);

                // Obfuscate the value by XORing it with the new key.
                _encryptedValue = floatBits ^ _obfuscationKey;
            }
        }

        /// <summary>
        /// Initializes a new instance of the SecureFloat with a starting value.
        /// </summary>
        /// <param name="initialValue">The starting float value.</param>
        public SecureFloat(float initialValue = 0)
        {
            _obfuscationKey = 0;
            _encryptedValue = 0;
            // The property setter handles the initial obfuscation.
            this.Value = initialValue;
        }

        //
        // Operator Overloads for seamless use with standard floats
        //

        // Implicit conversion from SecureFloat to float
        public static implicit operator float(SecureFloat secureFloat)
        {
            return secureFloat.Value;
        }

        // Implicit conversion from float to SecureFloat
        public static implicit operator SecureFloat(float f)
        {
            return new SecureFloat(f);
        }

        // Addition operator
        public static SecureFloat operator +(SecureFloat a, SecureFloat b)
        {
            return new SecureFloat(a.Value + b.Value);
        }

        // Subtraction operator
        public static SecureFloat operator -(SecureFloat a, SecureFloat b)
        {
            return new SecureFloat(a.Value - b.Value);
        }

        // Multiplication operator
        public static SecureFloat operator *(SecureFloat a, SecureFloat b)
        {
            return new SecureFloat(a.Value * b.Value);
        }

        // Division operator
        public static SecureFloat operator /(SecureFloat a, SecureFloat b)
        {
            if (b.Value == 0)
            {
                throw new DivideByZeroException("Cannot divide SecureFloat by zero.");
            }
            return new SecureFloat(a.Value / b.Value);
        }

        // Equality operator
        public static bool operator ==(SecureFloat a, SecureFloat b)
        {
            return Mathf.Approximately(a.Value, b.Value);
        }

        // Inequality operator
        public static bool operator !=(SecureFloat a, SecureFloat b)
        {
            return !Mathf.Approximately(a.Value, b.Value);
        }

        // Comparison operators
        public static bool operator <(SecureFloat a, SecureFloat b)
        {
            return a.Value < b.Value;
        }

        public static bool operator >(SecureFloat a, SecureFloat b)
        {
            return a.Value > b.Value;
        }

        public static bool operator <=(SecureFloat a, SecureFloat b)
        {
            return a.Value <= b.Value;
        }

        public static bool operator >=(SecureFloat a, SecureFloat b)
        {
            return a.Value >= b.Value;
        }

        // Override methods for standard C# object behavior
        public override bool Equals(object obj)
        {
            if (obj is SecureFloat)
            {
                return this.Value.Equals(((SecureFloat)obj).Value);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public override string ToString()
        {
            return this.Value.ToString();
        }
    }
}
