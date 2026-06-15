using System;
using System.Globalization;
using System.Net;

namespace ModMenu
{
    public partial class ModMenuBehaviour
    {
        // Describes ownership for the selected profile instead of the menu process
        private static string GetPlayerAuthorityLabel(PlayerProfile playerProfile, bool isHost)
        {
            if (playerProfile.isLocalPlayer)
            {
                return isHost ? "Host player" : "Local client";
            }

            return isHost ? "Remote client" : "Remote player";
        }

        // Returns an IP only when the active transport exposes a real network address
        private string GetConnectionEndpoint(
            PlayerOrgans playerOrgans,
            PlayerProfile playerProfile,
            out string endpointLabel)
        {
            endpointLabel = "Endpoint";
            try
            {
                if (playerProfile != null && playerProfile.isLocalPlayer)
                {
                    return "Local process (no remote IP)";
                }

                string address = playerProfile != null && playerProfile.connectionToClient != null
                    ? playerProfile.connectionToClient.address
                    : playerOrgans != null && playerOrgans.connectionToClient != null
                        ? playerOrgans.connectionToClient.address
                        : string.Empty;
                // Mirror transports may return an IP, a relay peer ID, or an opaque endpoint label
                if (!string.IsNullOrWhiteSpace(address))
                {
                    if (TryNormalizeIpAddress(address, out string ipAddress))
                    {
                        endpointLabel = "IP Address";
                        return ipAddress;
                    }

                    // FizzySteamworks returns a Steam peer ID because relay networking hides IPs
                    if (ulong.TryParse(address, NumberStyles.None, CultureInfo.InvariantCulture, out ulong peerId) &&
                        peerId != 0uL)
                    {
                        return "Steam relay (IP hidden)";
                    }

                    return address;
                }
            }
            catch (Exception ex)
            {
                // Connection replacement can briefly invalidate the transport object
                ModMenuLoader.Log("Connection endpoint read error: " + ex.Message);
            }

            return IsHostAddressAvailable(playerProfile) ? "Transport hides remote IP" : "Unavailable";
        }

        // Accepts plain IPs and common host-port forms without treating Steam IDs as addresses
        private static bool TryNormalizeIpAddress(string endpoint, out string ipAddress)
        {
            string candidate = endpoint.Trim();
            if (IPAddress.TryParse(candidate, out IPAddress? parsedAddress))
            {
                ipAddress = parsedAddress.ToString();
                return true;
            }

            if (candidate.StartsWith('['))
            {
                int closingBracket = candidate.IndexOf(']', StringComparison.Ordinal);
                if (closingBracket > 1 &&
                    IPAddress.TryParse(candidate.AsSpan(1, closingBracket - 1), out parsedAddress))
                {
                    ipAddress = parsedAddress.ToString();
                    return true;
                }
            }

            int finalColon = candidate.LastIndexOf(':');
            if (finalColon > 0 &&
                candidate.IndexOf(':', StringComparison.Ordinal) == finalColon &&
                IPAddress.TryParse(candidate.AsSpan(0, finalColon), out parsedAddress))
            {
                ipAddress = parsedAddress.ToString();
                return true;
            }

            ipAddress = string.Empty;
            return false;
        }

        // Distinguishes a hidden server address from a missing client connection
        private bool IsHostAddressAvailable(PlayerProfile? playerProfile)
        {
            return cachedGM != null &&
                cachedGM.isServer &&
                playerProfile != null &&
                !playerProfile.isLocalPlayer;
        }
    }
}
