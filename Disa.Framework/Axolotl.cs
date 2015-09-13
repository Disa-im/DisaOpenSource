using System;
using System.IO;
using System.Globalization;
using SQLite;
using System.Linq;
using System.Collections.Generic;

namespace Disa.Framework
{
    public class Axolotl
    {
        private static class Database
        {
            private static string GetPath(int registrationId)
            {
                return Path.Combine(Platform.GetSettingsPath(), "axolotl-" + registrationId.ToString(CultureInfo.InvariantCulture));
            }

            public static bool Exists(int registrationId)
            {
                return File.Exists(GetPath(registrationId));
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
            public int Id
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public byte[] Data
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }

        public class SignedPreKey
        {
            public int Id
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public byte[] Data
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public byte[] Signature
            {
                get
                {
                    throw new NotImplementedException();
                }
            }
        }

        public enum MessageType { PreKeyMessage, Message }

        public class Message
        {
            public MessageType Type { get; set; }
            public byte[] Data { get; set; }
        }

        public class NoDatabaseFound : System.Exception
        {
            public NoDatabaseFound() : base()
            {
            }

            public NoDatabaseFound(string message) : base(message)
            {
            }
        }

        public class InvalidKeyIdException : System.Exception
        {
            public InvalidKeyIdException() : base()
            {
            }

            public InvalidKeyIdException(string message) : base(message)
            {
            }
        }

        public class InvalidKeyException : System.Exception
        {
            public InvalidKeyException() : base()
            {
            }

            public InvalidKeyException(string message) : base(message)
            {
            }
        }

        public class UntrustedIdentityException : System.Exception
        {
            public UntrustedIdentityException() : base()
            {
            }

            public UntrustedIdentityException(string message) : base(message)
            {
            }
        }

        public class LegacyMessageException : System.Exception
        {
            public LegacyMessageException() : base()
            {
            }

            public LegacyMessageException(string message) : base(message)
            {
            }
        }

        public class DuplicateMessageException : System.Exception
        {
            public DuplicateMessageException() : base()
            {
            }

            public DuplicateMessageException(string message) : base(message)
            {
            }
        }

        public class InvalidMessageException : System.Exception
        {
            public InvalidMessageException() : base()
            {
            }

            public InvalidMessageException(string message) : base(message)
            {
            }
        }

        public class NoSessionException : System.Exception
        {
            public NoSessionException() : base()
            {
            }

            public NoSessionException(string message) : base(message)
            {
            }
        }
            
        public Axolotl(int registrationId)
        {

        }

        private void ProcessException(Exception ex)
        {
            throw new NotImplementedException();
        }

        public Message EncryptMessage(string address, int deviceId, byte[] data)
        {
            throw new NotImplementedException();
        }

        public bool HasSession(string address, int deviceId)
        {
            throw new NotImplementedException();
        }

        public void ProcessPreKey(string address, int deviceId, PreKeyBundle bundle)
        {
            throw new NotImplementedException();
        }

        public byte[] DecryptMessage(string address, int deviceId, Message message)
        {
            throw new NotImplementedException();
        }

        public byte[] DecryptPreKeyMessage(string address, int deviceId, byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] DecryptMessage(string address, int deviceId, byte[] data)
        {
            throw new NotImplementedException();
        }

        public byte[] IdentityKeyPair
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public SignedPreKey GenerateSignedPreKey()
        {
            throw new NotImplementedException();
        }

        public PreKey[] GeneratePreKeys(int count)
        {
            throw new NotImplementedException();
        }

        public void StorePreKeys(PreKey[] preKeys)
        {
            throw new NotImplementedException();
        }

        public void StoreSignedPreKey(SignedPreKey signedPreKey)
        {
            throw new NotImplementedException();
        }
            
        public PreKey[] GetNewestPreKeys(int count)
        {
            throw new NotImplementedException();
        }

        public SignedPreKey GetNewestSignedPreKey()
        {
            throw new NotImplementedException();
        }

        public bool NeedsKeys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int RegistrationId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}

