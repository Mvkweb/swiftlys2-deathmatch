using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2_Deathmatch.Interfaces;
using System;

namespace SwiftlyS2_Deathmatch.Services;

public sealed class MessageSuppressionService : IMessageSuppressionService
{
    private readonly ISwiftlyCore _core;
    private Guid? _textMsgHookId;
    private Guid? _sayText2HookId;
    private Guid? _radioTextHookId;
    private Guid? _hudTextHookId;
    private Guid? _hudMsgHookId;

    public MessageSuppressionService(ISwiftlyCore core)
    {
        _core = core;
    }

    public void Register()
    {
        _textMsgHookId = _core.NetMessage.HookServerMessage<CUserMessageTextMsg>(OnServerTextMessage);
        _sayText2HookId = _core.NetMessage.HookServerMessage<CUserMessageSayText2>(OnServerSayText2);
        _radioTextHookId = _core.NetMessage.HookServerMessage<CCSUsrMsg_RadioText>(OnServerRadioText);
        _hudTextHookId = _core.NetMessage.HookServerMessage<CCSUsrMsg_HudText>(OnServerHudText);
        _hudMsgHookId = _core.NetMessage.HookServerMessage<CCSUsrMsg_HudMsg>(OnServerHudMsg);
    }

    public void Unregister()
    {
        if (_textMsgHookId.HasValue)
        {
            _core.NetMessage.Unhook(_textMsgHookId.Value);
            _textMsgHookId = null;
        }

        if (_sayText2HookId.HasValue)
        {
            _core.NetMessage.Unhook(_sayText2HookId.Value);
            _sayText2HookId = null;
        }

        if (_radioTextHookId.HasValue)
        {
            _core.NetMessage.Unhook(_radioTextHookId.Value);
            _radioTextHookId = null;
        }

        if (_hudTextHookId.HasValue)
        {
            _core.NetMessage.Unhook(_hudTextHookId.Value);
            _hudTextHookId = null;
        }

        if (_hudMsgHookId.HasValue)
        {
            _core.NetMessage.Unhook(_hudMsgHookId.Value);
            _hudMsgHookId = null;
        }
    }

    private static bool ShouldSuppressToken(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;

        var normalized = token.Trim();
        var t = normalized.StartsWith('#') ? normalized[1..] : normalized;

        return t.Contains("Player_Point_Award_", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("points for neutralizing", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("points for", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("assisting in neutralizing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSuppressPlainText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        var t = text.Trim();
        return t.Contains("points for neutralizing", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("points for assisting", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("points for", StringComparison.OrdinalIgnoreCase) ||
               t.Contains("assisting in neutralizing", StringComparison.OrdinalIgnoreCase);
    }

    private HookResult OnServerTextMessage(CUserMessageTextMsg msg)
    {
        if (msg.Param.Count == 0) return HookResult.Continue;

        var count = msg.Param.Count;
        for (int i = 0; i < count; i++)
        {
            var param = msg.Param[i];
            if (ShouldSuppressToken(param) || ShouldSuppressPlainText(param))
            {
                msg.Recipients.RemoveAllPlayers();
                return HookResult.Continue;
            }
        }
        return HookResult.Continue;
    }

    private HookResult OnServerSayText2(CUserMessageSayText2 msg)
    {
        var tokens = new[]
        {
            msg.Messagename,
            msg.Param1,
            msg.Param2,
            msg.Param3,
            msg.Param4,
        };

        foreach (var t in tokens)
        {
            if (string.IsNullOrEmpty(t)) continue;

            if (ShouldSuppressToken(t) || ShouldSuppressPlainText(t))
            {
                msg.Recipients.RemoveAllPlayers();
                return HookResult.Continue;
            }
        }
        return HookResult.Continue;
    }

    private HookResult OnServerRadioText(CCSUsrMsg_RadioText msg)
    {
        if (ShouldSuppressToken(msg.MsgName) || ShouldSuppressPlainText(msg.MsgName))
        {
            msg.Recipients.RemoveAllPlayers();
            return HookResult.Continue;
        }

        var count = msg.Params.Count;
        for (int i = 0; i < count; i++)
        {
            var p = msg.Params[i];
            if (ShouldSuppressToken(p) || ShouldSuppressPlainText(p))
            {
                msg.Recipients.RemoveAllPlayers();
                return HookResult.Continue;
            }
        }
        return HookResult.Continue;
    }

    private HookResult OnServerHudText(CCSUsrMsg_HudText msg)
    {
        if (ShouldSuppressToken(msg.Text) || ShouldSuppressPlainText(msg.Text))
        {
            msg.Recipients.RemoveAllPlayers();
        }
        return HookResult.Continue;
    }

    private HookResult OnServerHudMsg(CCSUsrMsg_HudMsg msg)
    {
        if (ShouldSuppressToken(msg.Text) || ShouldSuppressPlainText(msg.Text))
        {
            msg.Recipients.RemoveAllPlayers();
        }
        return HookResult.Continue;
    }
}
