using System.Net;
using Microsoft.Extensions.Logging;

namespace HanZombiePlagueS2;

public sealed class HZPBroadcastGeoIpService(ILogger<HZPBroadcastGeoIpService> logger)
{
    private MmdbReader? _reader;
    private bool _initialized;

    public bool Initialize(string databasePath)
    {
        if (_initialized)
        {
            return _reader != null;
        }

        _initialized = true;

        try
        {
            if (!File.Exists(databasePath))
            {
                logger.LogWarning("[CountryAnnounce] GeoIP database not found at {Path}", databasePath);
                return false;
            }

            _reader = new MmdbReader(databasePath);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CountryAnnounce] Failed to initialize GeoIP database");
            return false;
        }
    }

    public string? GetCountryCode(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || _reader == null)
        {
            return null;
        }

        string ip = ExtractIpAddress(ipAddress);
        if (IsPrivateIp(ip))
        {
            return null;
        }

        try
        {
            return _reader.GetCountryCode(ip);
        }
        catch
        {
            return null;
        }
    }

    private static string ExtractIpAddress(string ipAddress)
    {
        int colonIndex = ipAddress.IndexOf(':');
        return colonIndex > 0 ? ipAddress[..colonIndex] : ipAddress;
    }

    private static bool IsPrivateIp(string ip)
    {
        return string.IsNullOrEmpty(ip)
               || ip == "0.0.0.0"
               || ip.StartsWith("127.")
               || ip.StartsWith("192.168.")
               || ip.StartsWith("10.")
               || ip.StartsWith("172.16.")
               || ip.StartsWith("172.17.")
               || ip.StartsWith("172.18.")
               || ip.StartsWith("172.19.")
               || ip.StartsWith("172.20.")
               || ip.StartsWith("172.21.")
               || ip.StartsWith("172.22.")
               || ip.StartsWith("172.23.")
               || ip.StartsWith("172.24.")
               || ip.StartsWith("172.25.")
               || ip.StartsWith("172.26.")
               || ip.StartsWith("172.27.")
               || ip.StartsWith("172.28.")
               || ip.StartsWith("172.29.")
               || ip.StartsWith("172.30.")
               || ip.StartsWith("172.31.");
    }

    private sealed class MmdbReader
    {
        private readonly byte[] _data;
        private readonly int _metadataStart;
        public int NodeCount { get; }
        public int RecordSize { get; }
        private readonly int _nodeByteSize;
        private readonly int _dataSectionStart;
        private readonly int _ipVersion;
        private static readonly byte[] MetadataMarker = { 0xAB, 0xCD, 0xEF, 0x4D, 0x61, 0x78, 0x4D, 0x69, 0x6E, 0x64, 0x2E, 0x63, 0x6F, 0x6D };

        public MmdbReader(string path)
        {
            _data = File.ReadAllBytes(path);
            _metadataStart = FindMetadataMarker();
            if (_metadataStart < 0)
                throw new InvalidDataException("Invalid MMDB file");

            int metaOffset = _metadataStart + MetadataMarker.Length;
            var metadata = DecodeValue(ref metaOffset) as Dictionary<string, object> ?? throw new InvalidDataException("Invalid MMDB metadata");
            NodeCount = Convert.ToInt32(metadata["node_count"]);
            RecordSize = Convert.ToInt32(metadata["record_size"]);
            _ipVersion = Convert.ToInt32(metadata["ip_version"]);
            _nodeByteSize = RecordSize * 2 / 8;
            _dataSectionStart = NodeCount * _nodeByteSize + 16;
        }

        public string? GetCountryCode(string ipString)
        {
            if (!IPAddress.TryParse(ipString, out var ip))
                return null;

            var bytes = ip.GetAddressBytes();
            if (_ipVersion == 6 && bytes.Length == 4)
            {
                var ipv6Bytes = new byte[16];
                ipv6Bytes[10] = 0xFF;
                ipv6Bytes[11] = 0xFF;
                Array.Copy(bytes, 0, ipv6Bytes, 12, 4);
                bytes = ipv6Bytes;
            }

            int node = 0;
            int bitCount = bytes.Length * 8;
            for (int i = 0; i < bitCount && node < NodeCount; i++)
            {
                int bit = (bytes[i >> 3] >> (7 - (i & 7))) & 1;
                node = ReadNode(node, bit);
            }

            if (node <= NodeCount)
                return null;

            int dataOffset = (node - NodeCount) - 16 + _dataSectionStart;
            if (dataOffset < 0 || dataOffset >= _metadataStart)
                return null;

            var result = DecodeValue(ref dataOffset);
            return ExtractCountryCode(result);
        }

        private int FindMetadataMarker()
        {
            for (int i = _data.Length - MetadataMarker.Length; i >= Math.Max(0, _data.Length - 131072); i--)
            {
                bool match = true;
                for (int j = 0; j < MetadataMarker.Length && match; j++)
                {
                    if (_data[i + j] != MetadataMarker[j])
                        match = false;
                }
                if (match) return i;
            }
            return -1;
        }

        private int ReadNode(int nodeNumber, int bit)
        {
            int offset = nodeNumber * _nodeByteSize;
            return RecordSize switch
            {
                24 => bit == 0 ? (_data[offset] << 16) | (_data[offset + 1] << 8) | _data[offset + 2] : (_data[offset + 3] << 16) | (_data[offset + 4] << 8) | _data[offset + 5],
                28 => bit == 0 ? (_data[offset] << 20) | (_data[offset + 1] << 12) | (_data[offset + 2] << 4) | (_data[offset + 3] >> 4) : ((_data[offset + 3] & 0x0F) << 24) | (_data[offset + 4] << 16) | (_data[offset + 5] << 8) | _data[offset + 6],
                32 => bit == 0 ? (_data[offset] << 24) | (_data[offset + 1] << 16) | (_data[offset + 2] << 8) | _data[offset + 3] : (_data[offset + 4] << 24) | (_data[offset + 5] << 16) | (_data[offset + 6] << 8) | _data[offset + 7],
                _ => NodeCount
            };
        }

        private object? DecodeValue(ref int offset)
        {
            if (offset >= _data.Length)
                return null;

            byte ctrl = _data[offset++];
            int type = ctrl >> 5;
            int size = ctrl & 0x1F;
            if (type == 0)
            {
                if (offset >= _data.Length) return null;
                type = _data[offset++] + 7;
            }

            size = size switch
            {
                < 29 => size,
                29 => 29 + _data[offset++],
                30 => 285 + (_data[offset++] << 8) + _data[offset++],
                _ => 65821 + (_data[offset++] << 16) + (_data[offset++] << 8) + _data[offset++]
            };

            return type switch
            {
                2 => DecodeString(size, ref offset),
                7 => DecodeMap(size, ref offset),
                11 => DecodeArray(size, ref offset),
                _ => Skip(size, ref offset)
            };
        }

        private string DecodeString(int size, ref int offset)
        {
            string value = System.Text.Encoding.UTF8.GetString(_data, offset, size);
            offset += size;
            return value;
        }

        private Dictionary<string, object> DecodeMap(int size, ref int offset)
        {
            var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < size; i++)
            {
                string? key = DecodeValue(ref offset) as string;
                object? value = DecodeValue(ref offset);
                if (!string.IsNullOrEmpty(key) && value != null)
                    map[key] = value;
            }
            return map;
        }

        private List<object?> DecodeArray(int size, ref int offset)
        {
            var list = new List<object?>(size);
            for (int i = 0; i < size; i++)
                list.Add(DecodeValue(ref offset));
            return list;
        }

        private object? Skip(int size, ref int offset)
        {
            offset += size;
            return null;
        }

        private static string? ExtractCountryCode(object? result)
        {
            if (result is not Dictionary<string, object> map)
                return null;

            if (map.TryGetValue("country", out var countryObj)
                && countryObj is Dictionary<string, object> countryMap
                && countryMap.TryGetValue("iso_code", out var isoCode))
            {
                return isoCode?.ToString()?.ToUpperInvariant();
            }

            if (map.TryGetValue("registered_country", out var registeredObj)
                && registeredObj is Dictionary<string, object> registeredMap
                && registeredMap.TryGetValue("iso_code", out var registeredCode))
            {
                return registeredCode?.ToString()?.ToUpperInvariant();
            }

            return null;
        }
    }
}
