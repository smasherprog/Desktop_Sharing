using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SSLTcp
{
    public class Secure_Base_Tcp
    {
        private string _Key;
        byte[] _MySessionKey;
        private int _Buffer_Size = 1024 * 1024 * 8;
        byte[] _Buffer;

        public Secure_Base_Tcp(string key)
        {
            _Buffer = new byte[_Buffer_Size];// 8 megabytes buffer
            _Key = key;

        }
        protected bool ExchangeKeys(NetworkStream stream)
        {
            try
            {
                using(ECDiffieHellmanCng alice = new ECDiffieHellmanCng())
                {
                    alice.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                    alice.HashAlgorithm = CngAlgorithm.Sha256;
                    using(RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.FromXmlString(_Key);
                        var data = rsa.Encrypt(alice.PublicKey.ToByteArray(), true);

                        byte[] intBytes = BitConverter.GetBytes(data.Length);
                        stream.Write(intBytes, 0, intBytes.Length);
                        stream.Write(data, 0, data.Length);

                        var b = new byte[4];
                        stream.Read(b, 0, 4);
                        var len = BitConverter.ToInt32(b, 0);
                        stream.Read(data, 0, data.Length);
                        var dec = rsa.Decrypt(data, true);
                        _MySessionKey = alice.DeriveKeyMaterial(CngKey.Import(dec, CngKeyBlobFormat.EccPublicBlob));
                        Debug.WriteLine("Key Exchange completed!");

                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
            return true;
        }
        protected void Write(byte[] data, NetworkStream stream)
        {
            try
            {
                using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.KeySize = _MySessionKey.Length;
                    aes.Key = _MySessionKey;
                    aes.GenerateIV();
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    var iv = new byte[16];
                    stream.Write(iv, 0, 16);
                    using(ICryptoTransform encrypt = aes.CreateEncryptor())
                    {
                        var encryptedbytes = encrypt.TransformFinalBlock(data, 0, data.Length);
                        stream.Write(BitConverter.GetBytes(encryptedbytes.Length), 0, 4);
                        stream.Write(encryptedbytes, 0, encryptedbytes.Length);
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        protected byte[] Read(NetworkStream stream)
        {
            try
            {
                if(stream.DataAvailable)
                {
                    var iv = new byte[16];
                    stream.Read(iv, 0, 16);

                    var b = new byte[4];
                    stream.Read(b, 0, 4);
                    var len = BitConverter.ToInt32(b, 0);

                    using(AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                    {
                        aes.KeySize = _MySessionKey.Length;
                        aes.Key = _MySessionKey;
                        aes.IV = iv;
                        aes.Mode = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;

                        using(ICryptoTransform decrypt = aes.CreateDecryptor())
                        {
                            return decrypt.TransformFinalBlock(_Buffer, 0, len);
                        }
                    }
                }
            } catch(Exception e)
            {
                Debug.WriteLine(e.Message);
            }
            return new byte[0];
        }
    }
}
