using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2_Deathmatch.Interfaces;
using SwiftlyS2_Deathmatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class SpawnEvaluatorService : ISpawnEvaluatorService
{
    private readonly ISwiftlyCore _core;
    private readonly IMapConfigService _mapConfig;
    private readonly Random _random = new();

    // Squared distances for performance (avoids Math.Sqrt)
    private const float MinSafeDistanceSq = 120f * 120f; // ~120 units: Hard collision boundary
    private const float OptimalMinDistanceSq = 600f * 600f; // Close to action
    private const float OptimalMaxDistanceSq = 1500f * 1500f; // But not too far
    private const float DangerCloseDistanceSq = 400f * 400f; // Very dangerous, high penalty

    public SpawnEvaluatorService(ISwiftlyCore core, IMapConfigService mapConfig)
    {
        _core = core;
        _mapConfig = mapConfig;
    }

    public Spawn? GetBestSpawn(IPlayer player, Team team)
    {
        var availableSpawns = _mapConfig.Spawns.Where(s => s.Team == team).ToList();
        if (availableSpawns.Count == 0) return null;

        // Fetch living players once to avoid overhead in the loop
        var livingPlayers = _core.PlayerManager.GetAllPlayers()
            .Where(p => p.IsValid && p.Slot != player.Slot && p.Controller.PawnIsAlive && p.PlayerPawn is not null && p.PlayerPawn.AbsOrigin is not null)
            .ToList();

        // If no one else is alive, just pick a random spawn (fast path)
        if (livingPlayers.Count == 0)
        {
            return availableSpawns[_random.Next(availableSpawns.Count)];
        }

        Spawn? bestSpawn = null;
        float bestScore = float.MinValue;

        // Fallback in case all spawns are slightly "unsafe" due to high player count
        var safeSpawns = new List<Spawn>();

        foreach (var spawn in availableSpawns)
        {
            var isColliding = false;
            float spawnScore = 0f;

            foreach (var other in livingPlayers)
            {
                var otherPos = other.PlayerPawn!.AbsOrigin!.Value;
                var otherAngles = other.PlayerPawn.AbsRotation;

                // 1. Absolute Collision Check
                float dx = spawn.Position.X - otherPos.X;
                float dy = spawn.Position.Y - otherPos.Y;
                float dz = spawn.Position.Z - otherPos.Z;
                float distSq = (dx * dx) + (dy * dy) + (dz * dz);

                if (distSq < MinSafeDistanceSq)
                {
                    isColliding = true;
                    break; // Immediate disqualification
                }

                // If it passed the collision check, we can at least consider it a fallback
                
                var otherTeam = (Team)other.Controller.TeamNum;
                if (otherTeam == Team.Spectator || otherTeam == Team.None) continue;

                // Enemies vs Friends logic (FFA DM vs Team DM)
                // For standard DM, everyone is an enemy if mp_teammates_are_enemies is 1.
                // Assuming standard FFA logic, we treat everyone as a potential threat.
                
                // 2. Combat Density Scoring
                if (distSq < DangerCloseDistanceSq)
                {
                    spawnScore -= 500f; // Heavy penalty for spawning right next to someone
                }
                else if (distSq >= OptimalMinDistanceSq && distSq <= OptimalMaxDistanceSq)
                {
                    spawnScore += 100f; // Reward for being in the "Goldilocks" zone
                }
                else if (distSq > OptimalMaxDistanceSq)
                {
                    spawnScore -= 20f; // Slight penalty for being too far from the action
                }

                // 3. Line of Sight / Angle Check (Anti-Spawn Camp)
                // Calculate if the 'other' player is looking towards this spawn
                if (otherAngles is not null)
                {
                    // Convert Yaw to a 2D forward vector
                    float yawRad = otherAngles.Value.Yaw * (MathF.PI / 180f);
                    float forwardX = MathF.Cos(yawRad);
                    float forwardY = MathF.Sin(yawRad);

                    // Normalize the vector pointing from the enemy to the spawn
                    float dist2D = MathF.Sqrt((dx * dx) + (dy * dy));
                    if (dist2D > 0)
                    {
                        float dirX = dx / dist2D;
                        float dirY = dy / dist2D;

                        // Dot product: 1 = looking exactly at, -1 = looking exactly away
                        float dot = (forwardX * dirX) + (forwardY * dirY);

                        // If they are looking towards the spawn (in front of them)
                        if (dot > 0.3f && distSq < (3000f * 3000f))
                        {
                            spawnScore -= (dot * 2000f); // Massive penalty for spawning in front of someone
                        }
                        // If they are looking directly away (you spawning behind their back)
                        else if (dot < -0.7f && distSq < DangerCloseDistanceSq)
                        {
                            spawnScore -= 800f; // Heavy penalty for spawning directly behind someone who is close
                        }
                    }
                }
            }

            if (isColliding) continue; // Skip to next spawn, this one is blocked

            safeSpawns.Add(spawn);

            // Add a tiny bit of randomness to avoid predictable spawn cycling
            spawnScore += _random.Next(-10, 11);

            if (spawnScore > bestScore)
            {
                bestScore = spawnScore;
                bestSpawn = spawn;
            }
        }

        // Return the highest scored spawn. If none passed the strict tests but some are "safe" from collision, pick a random safe one.
        if (bestSpawn is not null) return bestSpawn;
        if (safeSpawns.Count > 0) return safeSpawns[_random.Next(safeSpawns.Count)];
        
        // Absolute worst case: The server is incredibly packed and literally every spawn has someone standing on it.
        return availableSpawns[_random.Next(availableSpawns.Count)];
    }
}
