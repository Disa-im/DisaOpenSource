using System;
using System.Collections.Generic;

namespace Disa.Framework
{
    public class Axolotl
    {
        public interface IAxolotlStatic
        {
            Tuple<byte[], byte[]> GenerateCurveKeyPair();

            byte[] CalculateCurveAgreement(byte[] publicKey, byte[] privateKey);

            bool VerifyCurveSignature(byte[] message, byte[] signature, byte[] publicKey);

            byte[] DeriveHKDFv3Secrets(byte[] inputKeyMaterial, byte[] info, int outputLength);
        }

        public interface IAxolotl
        {
            void Constructor(int registrationId);

            Message EncryptMessage(string address, int deviceId, byte[] data);

            Message EncryptSenderKeyMessage(string groupId, string address, int deviceId, byte[] data);

            bool HasSession(string address, int deviceId);

            List<Tuple<string, int>> HasSessions(List<Tuple<string, int>> addresses);

            void ProcessPreKey(string address, int deviceId, PreKeyBundle bundle);

            void ProcessSenderKey(string groupId, string address, int deviceId, byte[] senderKeyDistributionMessage);

            byte[] CreateSenderKey(string groupId, string address, int deviceId);

            byte[] DecryptMessage(string groupId, string address, int deviceId, Message message);

            byte[] DecryptPreKeyMessage(string address, int deviceId, byte[] data);

            byte[] DecryptSenderKeyMessage(string groupId, string address, int deviceId, byte[] data);

            byte[] DecryptMessage(string address, int deviceId, byte[] data);

            byte[] IdentityKeyPair
            {
                get;
            }

            SignedPreKey GenerateSignedPreKey();

            PreKey[] GeneratePreKeys(int count);

            void StorePreKeys(PreKey[] preKeys);

            void StoreSignedPreKey(SignedPreKey signedPreKey);

            PreKey[] GetNewestPreKeys(int count);

            SignedPreKey GetNewestSignedPreKey();

            bool NeedsKeys
            {
                get;
            }

            int RegistrationId
            {
                get;
            }
        }

        public class PreKeyBundle
        {
            public int RegistrationId { get; set; }
            public int DeviceId { get; set; }
            public int PreKeyId { get; set; }
            public byte[] PreKeyPublic { get; set; }
            public int SignedPreKeyId { get; set; }
            public byte[] SignedPreKeyPublic { get; set; }
            public byte[] SignedPreKeySignature { get; set; }
            public byte[] IdentityKey { get; set; }
        }

        public class PreKey
        {
            private readonly IPreKey _preKey;

            public interface IPreKey
            {
                int Id
                {
                    get;
                }
                byte[] Data
                {
                    get;
                }
                object Object
                {
                    get;
                }
            }

            public PreKey(IPreKey preKey)
            {
                _preKey = preKey;
            }

            public int Id
            {
                get
                {
                    return _preKey.Id;
                }
            }

            public byte[] Data
            {
                get
                {
                    return _preKey.Data;
                }
            }

            public object Object
            {
                get
                {
                    return _preKey.Object;
                }
            }
        }

        public class SignedPreKey
        {
            private readonly ISignedPreKey _signedPreKey;

            public interface ISignedPreKey
            {
                int Id
                {
                    get;
                }
                byte[] Data
                {
                    get;
                }
                byte[] Signature
                {
                    get;
                }
                object Object
                {
                    get;
                }
            }

            public SignedPreKey(ISignedPreKey signedPreKey)
            {
                _signedPreKey = signedPreKey;
            }

            public int Id
            {
                get
                {
                    return _signedPreKey.Id;
                }
            }

            public byte[] Data
            {
                get
                {
                    return _signedPreKey.Data;
                }
            }

            public byte[] Signature
            {
                get
                {
                    return _signedPreKey.Signature;
                }
            }

            public object Object
            {
                get
                {
                    return _signedPreKey.Object;
                }
            }
        }

        public enum MessageType { PreKeyMessage, Message, SenderKeyMessage }

        public class Message
        {
            public MessageType Type { get; set; }
            public byte[] Data { get; set; }
        }

        public class NoDatabaseFound : Exception
        {
            public NoDatabaseFound() : base()
            {
            }

            public NoDatabaseFound(string message) : base(message)
            {
            }
        }

        public class InvalidKeyIdException : Exception
        {
            public InvalidKeyIdException() : base()
            {
            }

            public InvalidKeyIdException(string message) : base(message)
            {
            }
        }

        public class InvalidKeyException : Exception
        {
            public InvalidKeyException() : base()
            {
            }

            public InvalidKeyException(string message) : base(message)
            {
            }
        }

        public class UntrustedIdentityException : Exception
        {
            public UntrustedIdentityException() : base()
            {
            }

            public UntrustedIdentityException(string message) : base(message)
            {
            }
        }

        public class LegacyMessageException : Exception
        {
            public LegacyMessageException() : base()
            {
            }

            public LegacyMessageException(string message) : base(message)
            {
            }
        }

        public class DuplicateMessageException : Exception
        {
            public DuplicateMessageException() : base()
            {
            }

            public DuplicateMessageException(string message) : base(message)
            {
            }
        }

        public class InvalidMessageException : Exception
        {
            public InvalidMessageException() : base()
            {
            }

            public InvalidMessageException(string message) : base(message)
            {
            }
        }

        public class NoSessionException : Exception
        {
            public NoSessionException() : base()
            {
            }

            public NoSessionException(string message) : base(message)
            {
            }
        }

        static internal AxolotlImplementation AxolotlImplementation { private get; set; }

        private readonly IAxolotl _axolotl;

        public Axolotl(int registrationId)
        {
            if (AxolotlImplementation == null || AxolotlImplementation.InstantianteAxolotl == null)
            {
                throw new Exception("No Axolotl implementation.");
            }
            _axolotl = AxolotlImplementation.InstantianteAxolotl();
            _axolotl.Constructor(registrationId);
        }

        public Message EncryptMessage(string address, int deviceId, byte[] data)
        {
            return _axolotl.EncryptMessage(address, deviceId, data);
        }

        public Message EncryptSenderKeyMessage(string groupId, string address, int deviceId, byte[] data)
        {
            return _axolotl.EncryptSenderKeyMessage(groupId, address, deviceId, data);
        }

        public bool HasSession(string address, int deviceId)
        {
            return _axolotl.HasSession(address, deviceId);
        }

        public List<Tuple<string, int>> HasSessions(List<Tuple<string, int>> addresses)
        {
            return _axolotl.HasSessions(addresses);
        }

        public void ProcessPreKey(string address, int deviceId, PreKeyBundle bundle)
        {
            _axolotl.ProcessPreKey(address, deviceId, bundle);
        }

        public void ProcessSenderKey(string groupId, string address, int deviceId, byte[] senderKeyDistributionMessage)
        {
            _axolotl.ProcessSenderKey(groupId, address, deviceId, senderKeyDistributionMessage);
        }

        public byte[] CreateSenderKey(string groupId, string address, int deviceId)
        {
            return _axolotl.CreateSenderKey(groupId, address, deviceId);
        }

        public byte[] DecryptMessage(string groupId, string address, int deviceId, Message message)
        {
            return _axolotl.DecryptMessage(groupId, address, deviceId, message);
        }

        public byte[] DecryptPreKeyMessage(string address, int deviceId, byte[] data)
        {
            return _axolotl.DecryptPreKeyMessage(address, deviceId, data);
        }

        public byte[] DecryptSenderKeyMessage(string groupId, string address, int deviceId, byte[] data)
        {
            return _axolotl.DecryptSenderKeyMessage(groupId, address, deviceId, data);
        }

        public byte[] DecryptMessage(string address, int deviceId, byte[] data)
        {
            return _axolotl.DecryptMessage(address, deviceId, data);
        }

        public byte[] IdentityKeyPair
        {
            get
            {
                return _axolotl.IdentityKeyPair;
            }
        }

        public SignedPreKey GenerateSignedPreKey()
        {
            return _axolotl.GenerateSignedPreKey();
        }

        public PreKey[] GeneratePreKeys(int count)
        {
            return _axolotl.GeneratePreKeys(count);
        }

        public void StorePreKeys(PreKey[] preKeys)
        {
            _axolotl.StorePreKeys(preKeys);
        }

        public void StoreSignedPreKey(SignedPreKey signedPreKey)
        {
            _axolotl.StoreSignedPreKey(signedPreKey);
        }
            
        public PreKey[] GetNewestPreKeys(int count)
        {
            return _axolotl.GetNewestPreKeys(count);
        }

        public SignedPreKey GetNewestSignedPreKey()
        {
            return _axolotl.GetNewestSignedPreKey();
        }

        public bool NeedsKeys
        {
            get
            {
                return _axolotl.NeedsKeys;
            }
        }

        public int RegistrationId
        {
            get
            {
                return _axolotl.RegistrationId;
            }
        }

        private static void CheckForAxolotlStaticImplementation()
        {
            if (AxolotlImplementation == null || AxolotlImplementation.AxolotlStatic == null)
            {
                throw new Exception("No AxolotlStatic implementation.");
            }
        }

        public static byte[] DeriveHKDFv3Secrets(byte[] inputKeyMaterial, byte[] info, int outputLength)
        {
            CheckForAxolotlStaticImplementation();
            return AxolotlImplementation.AxolotlStatic.DeriveHKDFv3Secrets(inputKeyMaterial, info, outputLength);
        }

        public static Tuple<byte[], byte[]> GenerateCurveKeyPair()
        {
            CheckForAxolotlStaticImplementation();
            return AxolotlImplementation.AxolotlStatic.GenerateCurveKeyPair();
        }

        public static byte[] CalculateCurveAgreement(byte[] publicKey, byte[] privateKey)
        {
            CheckForAxolotlStaticImplementation();
            return AxolotlImplementation.AxolotlStatic.CalculateCurveAgreement(publicKey, privateKey);
        }

        public static bool VerifyCurveSignature(byte[] message, byte[] signature, byte[] publicKey)
        {
            CheckForAxolotlStaticImplementation();
            return AxolotlImplementation.AxolotlStatic.VerifyCurveSignature(message, signature, publicKey);
        }
    }
}

