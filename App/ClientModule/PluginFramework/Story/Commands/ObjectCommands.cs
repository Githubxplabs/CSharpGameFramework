﻿using System;
using System.Collections;
using System.Collections.Generic;
using StorySystem;
using ScriptRuntime;
using GameFramework;
using GameFramework.Skill;

namespace GameFramework.Story.Commands
{
    /// <summary>
    /// objface(obj_id, dir[, immediately]);
    /// </summary>
    internal class ObjFaceCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjFaceCommand cmd = new ObjFaceCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Dir = m_Dir.Clone();
            cmd.m_Immediately = m_Immediately.Clone();
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Dir.Evaluate(instance, iterator, args);
            m_Immediately.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            float dir = m_Dir.Value;
            int im = 0;
            if(m_Immediately.HaveValue)
                im = m_Immediately.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                MovementStateInfo msi = obj.GetMovementStateInfo();
                if (im != 0) {
                    msi.SetFaceDir(dir);

                    var uobj = PluginFramework.Instance.GetGameObject(objId);
                    if (null != uobj) {
                        var e = uobj.transform.eulerAngles;
                        uobj.transform.eulerAngles = new UnityEngine.Vector3(e.x, Geometry.RadianToDegree(dir), e.z);
                    }
                } else {
                    msi.SetWantedFaceDir(dir);
                }
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Dir.InitFromDsl(callData.GetParam(1));
                if (num > 2)
                    m_Immediately.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<float> m_Dir = new StoryValue<float>();
        private IStoryValue<int> m_Immediately = new StoryValue<int>();
    }
    /// <summary>
    /// objmove(obj_id, vector3(x,y,z)[, event]);
    /// </summary>
    internal class ObjMoveCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjMoveCommand cmd = new ObjMoveCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Pos = m_Pos.Clone();
            cmd.m_Event = m_Event.Clone();
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Pos.Evaluate(instance, iterator, args);
            m_Event.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            Vector3 pos = m_Pos.Value;
            string eventName = m_Event.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                List<Vector3> waypoints = new List<Vector3>();
                waypoints.Add(pos);
                AiStateInfo aiInfo = obj.GetAiStateInfo();
                AiData_ForMoveCommand data = aiInfo.AiDatas.GetData<AiData_ForMoveCommand>();
                if (null == data) {
                    data = new AiData_ForMoveCommand(waypoints);
                    aiInfo.AiDatas.AddData(data);
                }
                data.WayPoints = waypoints;
                data.Index = 0;
                data.IsFinish = false;
                data.Event = eventName;
                obj.GetMovementStateInfo().TargetPosition = pos;
                aiInfo.Time = 1000;//下一帧即触发移动
                aiInfo.ChangeToState((int)PredefinedAiStateId.MoveCommand);
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Pos.InitFromDsl(callData.GetParam(1));
                if (num > 2)
                    m_Event.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<Vector3> m_Pos = new StoryValue<Vector3>();
        private IStoryValue<string> m_Event = new StoryValue<string>();
    }
    /// <summary>
    /// objmovewithwaypoints(obj_id, vector3list("1 2 3 4 5 6")[, event]);
    /// </summary>
    internal class ObjMoveWithWaypointsCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjMoveWithWaypointsCommand cmd = new ObjMoveWithWaypointsCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_WayPoints = m_WayPoints.Clone();
            cmd.m_Event = m_Event.Clone();
            return cmd;
        }
        protected override void ResetState()
        {
        }
        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_WayPoints.Evaluate(instance, iterator, args);
            m_Event.Evaluate(instance, iterator, args);
        }
        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            List<object> poses = m_WayPoints.Value;
            string eventName = m_Event.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj && null != poses && poses.Count > 0) {
                List<Vector3> waypoints = new List<Vector3>();
                waypoints.Add(obj.GetMovementStateInfo().GetPosition3D());
                for (int i = 0; i < poses.Count; ++i) {
                    Vector3 pt = (Vector3)poses[i];
                    waypoints.Add(pt);
                }
                AiStateInfo aiInfo = obj.GetAiStateInfo();
                AiData_ForMoveCommand data = aiInfo.AiDatas.GetData<AiData_ForMoveCommand>();
                if (null == data) {
                    data = new AiData_ForMoveCommand(waypoints);
                    aiInfo.AiDatas.AddData(data);
                }
                data.WayPoints = waypoints;
                data.Index = 0;
                data.IsFinish = false;
                data.Event = eventName;
                obj.GetMovementStateInfo().TargetPosition = waypoints[0];
                aiInfo.Time = 1000;//下一帧即触发移动
                aiInfo.ChangeToState((int)PredefinedAiStateId.MoveCommand);
            }
            return false;
        }
        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_WayPoints.InitFromDsl(callData.GetParam(1));
                if (num > 2)
                    m_Event.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<List<object>> m_WayPoints = new StoryValue<List<object>>();
        private IStoryValue<string> m_Event = new StoryValue<string>();
    }
    /// <summary>
    /// objstop(obj_id);
    /// </summary>
    internal class ObjStopCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjStopCommand cmd = new ObjStopCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                AiStateInfo aiInfo = obj.GetAiStateInfo();
                if (aiInfo.CurState == (int)PredefinedAiStateId.MoveCommand) {
                    aiInfo.Time = 0;
                    aiInfo.Target = 0;
                }
                obj.GetMovementStateInfo().IsMoving = false;
                if (aiInfo.CurState > (int)PredefinedAiStateId.Invalid)
                    aiInfo.ChangeToState((int)PredefinedAiStateId.Idle);
            }
            EntityViewModel viewModel = EntityController.Instance.GetEntityViewById(objId);
            if (null != viewModel) {
                viewModel.StopMove();
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
    }
    /// <summary>
    /// objattack(npc_obj_id[,target_obj_id]);
    /// </summary>
    internal class ObjAttackCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjAttackCommand cmd = new ObjAttackCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_TargetObjId = m_TargetObjId.Clone();
            cmd.m_ParamNum = m_ParamNum;
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_TargetObjId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            EntityInfo entity = PluginFramework.Instance.GetEntityById(objId);
            EntityInfo target = null;
            int targetObjId = m_TargetObjId.Value;
            if (null != entity && null != target) {
                AiStateInfo aiInfo = entity.GetAiStateInfo();
                aiInfo.Target = targetObjId;
                aiInfo.LastChangeTargetTime = TimeUtility.GetLocalMilliseconds();
                aiInfo.ChangeToState((int)PredefinedAiStateId.Idle);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            m_ParamNum = callData.GetParamNum();
            if (m_ParamNum > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_TargetObjId.InitFromDsl(callData.GetParam(1));
            }
        }

        private int m_ParamNum = 0;
        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_TargetObjId = new StoryValue<int>();
    }
    /// <summary>
    /// objsetformation(obj_id, index);
    /// </summary>
    internal class ObjSetFormationCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetFormationCommand cmd = new ObjSetFormationCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_FormationIndex = m_FormationIndex.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_FormationIndex.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                obj.GetMovementStateInfo().FormationIndex = m_FormationIndex.Value;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_FormationIndex.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_FormationIndex = new StoryValue<int>();
    }
    /// <summary>
    /// objenableai(obj_id, true_or_false);
    /// </summary>
    internal class ObjEnableAiCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjEnableAiCommand cmd = new ObjEnableAiCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Enable = m_Enable.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Enable.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            string enable = m_Enable.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                obj.SetAIEnable(m_Enable.Value != "false");
            }
            EntityViewModel viewModel = EntityController.Instance.GetEntityViewById(objId);
            if (null != viewModel) {
                viewModel.StopMove();
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Enable.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<string> m_Enable = new StoryValue<string>();
    }
    /// <summary>
    /// objsetai(objid,ai_logic_id,stringlist("param1 param2 param3 ..."));
    /// </summary>
    internal class ObjSetAiCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetAiCommand cmd = new ObjSetAiCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_AiLogic = m_AiLogic.Clone();
            cmd.m_AiParams = m_AiParams.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_AiLogic.Evaluate(instance, iterator, args);
            m_AiParams.Evaluate(instance, iterator, args);

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            string aiLogic = m_AiLogic.Value;
            IEnumerable aiParams = m_AiParams.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.GetAiStateInfo().Reset();
                charObj.GetAiStateInfo().AiLogic = aiLogic;
                int ix = 0;
                foreach (string aiParam in aiParams) {
                    if (ix < AiStateInfo.c_MaxAiParamNum) {
                        charObj.GetAiStateInfo().AiParam[ix] = aiParam;
                        ++ix;
                    } else {
                        break;
                    }
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_AiLogic.InitFromDsl(callData.GetParam(1));
                m_AiParams.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<string> m_AiLogic = new StoryValue<string>();
        private IStoryValue<IEnumerable> m_AiParams = new StoryValue<IEnumerable>();
    }
    /// <summary>
    /// objsetaitarget(objid,targetid);
    /// </summary>
    internal class ObjSetAiTargetCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetAiTargetCommand cmd = new ObjSetAiTargetCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_TargetId = m_TargetId.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_TargetId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int targetId = m_TargetId.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.GetAiStateInfo().Target = targetId;
                charObj.GetAiStateInfo().HateTarget = targetId;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 2) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_TargetId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_TargetId = new StoryValue<int>();
    }
    /// <summary>
    /// objanimation(obj_id, anim[, normalized_time]);
    /// </summary>
    internal class ObjAnimationCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjAnimationCommand cmd = new ObjAnimationCommand();
            cmd.m_ParamNum = m_ParamNum;
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Anim = m_Anim.Clone();
            cmd.m_Time = m_Time.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Anim.Evaluate(instance, iterator, args);
            if (m_ParamNum > 2) {
                m_Time.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            string anim = m_Anim.Value;
            UnityEngine.GameObject obj = EntityController.Instance.GetGameObject(objId);
            if (null != obj) {
                UnityEngine.Animator animator = obj.GetComponentInChildren<UnityEngine.Animator>();
                if (null != animator) {
                    float time = 0;
                    if (m_ParamNum > 2) {
                        time = m_Time.Value;
                    }
                    animator.Play(anim, -1, time);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            m_ParamNum = callData.GetParamNum();
            if (m_ParamNum > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Anim.InitFromDsl(callData.GetParam(1));
            }
            if (m_ParamNum > 2) {
                m_Time.InitFromDsl(callData.GetParam(2));
            }
        }

        private int m_ParamNum = 0;
        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<string> m_Anim = new StoryValue<string>();
        private IStoryValue<float> m_Time = new StoryValue<float>();
    }
    /// <summary>
    /// objanimationparam(obj_id)
    /// {
    ///     float(name,val);
    ///     int(name,val);
    ///     bool(name,val);
    ///     trigger(name,val);
    /// };
    /// </summary>
    internal class ObjAnimationParamCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjAnimationParamCommand cmd = new ObjAnimationParamCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            for (int i = 0; i < m_Params.Count; ++i) {
                ParamInfo param = new ParamInfo();
                param.CopyFrom(m_Params[i]);
                cmd.m_Params.Add(param);
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Params.Count; ++i) {
                var pair = m_Params[i];
                pair.Key.Evaluate(instance, iterator, args);
                pair.Value.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            UnityEngine.GameObject obj = EntityController.Instance.GetGameObject(objId);
            if (null != obj) {
                UnityEngine.Animator animator = obj.GetComponentInChildren<UnityEngine.Animator>();
                if (null != animator) {
                    for (int i = 0; i < m_Params.Count; ++i) {
                        var param = m_Params[i];
                        string type = param.Type;
                        string key = param.Key.Value;
                        object val = param.Value.Value;
                        if (type == "int") {
                            int v = (int)Convert.ChangeType(val, typeof(int));
                            animator.SetInteger(key, v);
                        } else if (type == "float") {
                            float v = (float)Convert.ChangeType(val, typeof(float));
                            animator.SetFloat(key, v);
                        } else if (type == "bool") {
                            bool v = (bool)Convert.ChangeType(val, typeof(bool));
                            animator.SetBool(key, v);
                        } else if (type == "trigger") {
                            string v = val.ToString();
                            if (v == "false") {
                                animator.ResetTrigger(key);
                            } else {
                                animator.SetTrigger(key);
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
            }
        }

        protected override void Load(Dsl.FunctionData funcData)
        {
            Dsl.CallData callData = funcData.Call;
            if (null != callData) {
                Load(callData);
                for (int i = 0; i < funcData.Statements.Count; ++i) {
                    Dsl.ISyntaxComponent statement = funcData.Statements[i];
                    Dsl.CallData stCall = statement as Dsl.CallData;
                    if (null != stCall && stCall.GetParamNum() >= 2) {
                        string id = stCall.GetId();
                        ParamInfo param = new ParamInfo(id, stCall.GetParam(0), stCall.GetParam(1));
                        m_Params.Add(param);
                    }
                }
            }
        }

        private class ParamInfo
        {
            internal string Type;
            internal IStoryValue<string> Key;
            internal IStoryValue<object> Value;

            internal ParamInfo()
            {
                Init();
            }
            internal ParamInfo(string type, Dsl.ISyntaxComponent keyDsl, Dsl.ISyntaxComponent valDsl)
                : this()
            {
                Type = type;
                Key.InitFromDsl(keyDsl);
                Value.InitFromDsl(valDsl);
            }
            internal void CopyFrom(ParamInfo other)
            {
                Type = other.Type;
                Key = other.Key.Clone();
                Value = other.Value.Clone();
            }

            private void Init()
            {
                Type = string.Empty;
                Key = new StoryValue<string>();
                Value = new StoryValue();
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private List<ParamInfo> m_Params = new List<ParamInfo>();
    }
    /// <summary>
    /// objaddimpact(obj_id, impactid, arg1, arg2, ...)[seq("@seq")];
    /// </summary>
    internal class ObjAddImpactCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjAddImpactCommand cmd = new ObjAddImpactCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_ImpactId = m_ImpactId.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            cmd.m_HaveSeq = m_HaveSeq;
            cmd.m_SeqVarName = m_SeqVarName.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_ImpactId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
            if (m_HaveSeq) {
                m_SeqVarName.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int impactId = m_ImpactId.Value;
            int seq = 0;
            Dictionary<string, object> locals = new Dictionary<string, object>();
            for (int i = 0; i < m_Args.Count - 1; i += 2) {
                string key = m_Args[i].Value as string;
                object val = m_Args[i + 1].Value;
                if (!string.IsNullOrEmpty(key)) {
                    locals.Add(key, val);
                }
            }
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                ImpactInfo impactInfo = new ImpactInfo(impactId);
                impactInfo.StartTime = TimeUtility.GetLocalMilliseconds();
                impactInfo.ImpactSenderId = objId;
                impactInfo.SkillId = 0;
                if (null != impactInfo.ConfigData) {
                    obj.GetSkillStateInfo().AddImpact(impactInfo);
                    seq = impactInfo.Seq;
                    GfxSkillSystem.Instance.StartSkill(objId, impactInfo.ConfigData, seq, locals);
                }
            }
            if (m_HaveSeq) {
                string varName = m_SeqVarName.Value;
                instance.SetVariable(varName, seq);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_ImpactId.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        protected override void Load(Dsl.StatementData statementData)
        {
            if (statementData.Functions.Count == 2) {
                Dsl.FunctionData first = statementData.First;
                Dsl.FunctionData second = statementData.Second;
                if (null != first && null != first.Call && null != second && null != second.Call) {
                    Load(first.Call);
                    LoadVarName(second.Call);
                }
            }
        }

        private void LoadVarName(Dsl.CallData callData)
        {
            if (callData.GetId() == "seq" && callData.GetParamNum() == 1) {
                m_SeqVarName.InitFromDsl(callData.GetParam(0));
                m_HaveSeq = true;
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_ImpactId = new StoryValue<int>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
        private bool m_HaveSeq = false;
        private IStoryValue<string> m_SeqVarName = new StoryValue<string>();
    }
    /// <summary>
    /// objremoveimpact(obj_id, seq);
    /// </summary>
    internal class ObjRemoveImpactCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjRemoveImpactCommand cmd = new ObjRemoveImpactCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Seq = m_Seq.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Seq.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int seq = m_Seq.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                ImpactInfo impactInfo = obj.GetSkillStateInfo().GetImpactInfoBySeq(seq);
                if (null != impactInfo) {
                    GfxSkillSystem.Instance.StopSkill(objId, impactInfo.ImpactId, seq, true);
                    obj.GetSkillStateInfo().RemoveImpact(seq);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Seq.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_Seq = new StoryValue<int>();
    }
    /// <summary>
    /// objcastskill(obj_id, skillid, arg1, arg2, ...);
    /// </summary>
    internal class ObjCastSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjCastSkillCommand cmd = new ObjCastSkillCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_SkillId = m_SkillId.Clone();
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                cmd.m_Args.Add(val.Clone());
            }
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_SkillId.Evaluate(instance, iterator, args);
            for (int i = 0; i < m_Args.Count; ++i) {
                IStoryValue<object> val = m_Args[i];
                val.Evaluate(instance, iterator, args);
            }
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int skillId = m_SkillId.Value;
            Dictionary<string, object> locals = new Dictionary<string, object>();
            for (int i = 0; i < m_Args.Count - 1; i += 2) {
                string key = m_Args[i].Value as string;
                object val = m_Args[i + 1].Value;
                if (!string.IsNullOrEmpty(key)) {
                    locals.Add(key, val);
                }
            }
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                SkillInfo skillInfo = obj.GetSkillStateInfo().GetSkillInfoById(skillId);
                if (null != skillInfo) {
                    GfxSkillSystem.Instance.StartSkill(objId, skillInfo.ConfigData, 0, locals);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_SkillId.InitFromDsl(callData.GetParam(1));
            }
            for (int i = 2; i < callData.GetParamNum(); ++i) {
                StoryValue val = new StoryValue();
                val.InitFromDsl(callData.GetParam(i));
                m_Args.Add(val);
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
        private List<IStoryValue<object>> m_Args = new List<IStoryValue<object>>();
    }
    /// <summary>
    /// objstopskill(obj_id);
    /// </summary>
    internal class ObjStopSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjStopSkillCommand cmd = new ObjStopSkillCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            GfxSkillSystem.Instance.StopAllSkill(objId, true);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 0) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
    }
    /// <summary>
    /// objaddskill(obj_id, skillid);
    /// </summary>
    internal class ObjAddSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjAddSkillCommand cmd = new ObjAddSkillCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_SkillId = m_SkillId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_SkillId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int skillId = m_SkillId.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                if (obj.GetSkillStateInfo().GetSkillInfoById(skillId) == null) {
                    obj.GetSkillStateInfo().AddSkill(new SkillInfo(skillId));
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_SkillId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
    }
    /// <summary>
    /// objremoveskill(obj_id, skillid);
    /// </summary>
    internal class ObjRemoveSkillCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjRemoveSkillCommand cmd = new ObjRemoveSkillCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_SkillId = m_SkillId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_SkillId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int skillId = m_SkillId.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                obj.GetSkillStateInfo().RemoveSkill(skillId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_SkillId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SkillId = new StoryValue<int>();
    }
    /// <summary>
    /// objlisten(unit_id, 消息类别, true_or_false);
    /// </summary>
    internal class ObjListenCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjListenCommand cmd = new ObjListenCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Event = m_Event.Clone();
            cmd.m_Enable = m_Enable.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Event.Evaluate(instance, iterator, args);
            m_Enable.Evaluate(instance, iterator, args);

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            string eventName = m_Event.Value;
            string enable = m_Enable.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                if (StoryListenFlagEnum.Damage == StoryListenFlagUtility.FromString(eventName)) {
                    if (0 == string.Compare(enable, "true"))
                        obj.AddStoryFlag(StoryListenFlagEnum.Damage);
                    else
                        obj.RemoveStoryFlag(StoryListenFlagEnum.Damage);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Event.InitFromDsl(callData.GetParam(1));
                m_Enable.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<string> m_Event = new StoryValue<string>();
        private IStoryValue<string> m_Enable = new StoryValue<string>();
    }
    /// <summary>
    /// setvisible(objid,value);
    /// </summary>
    internal class SetVisibleCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetVisibleCommand cmd = new SetVisibleCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int value = m_Value.Value;
            EntityViewModel view = EntityController.Instance.GetEntityViewById(objId);
            if (null != view) {
                view.Visible = value != 0;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_Value = new StoryValue<int>();
    }
    /// <summary>
    /// sethp(objid,value);
    /// </summary>
    internal class SetHpCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetHpCommand cmd = new SetHpCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int value = m_Value.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.Hp = value;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_Value = new StoryValue<int>();
    }
    /// <summary>
    /// setenergy(objid,value);
    /// </summary>
    internal class SetEnergyCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetEnergyCommand cmd = new SetEnergyCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int value = m_Value.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.Energy = value;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_Value = new StoryValue<int>();
    }
    /// <summary>
    /// objset(uniqueid,localname,value);
    /// </summary>
    internal class ObjSetCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetCommand cmd = new ObjSetCommand();
            cmd.m_UniqueId = m_UniqueId.Clone();
            cmd.m_AttrName = m_AttrName.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_UniqueId.Evaluate(instance, iterator, args);
            m_AttrName.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int uniqueId = m_UniqueId.Value;
            string localName = m_AttrName.Value;
            object value = m_Value.Value;
            PluginFramework.Instance.SceneContext.ObjectSet(uniqueId, localName, value);
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_UniqueId.InitFromDsl(callData.GetParam(0));
                m_AttrName.InitFromDsl(callData.GetParam(1));
                m_Value.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_UniqueId = new StoryValue<int>();
        private IStoryValue<string> m_AttrName = new StoryValue<string>();
        private IStoryValue<object> m_Value = new StoryValue();
    }
    /// <summary>
    /// setlevel(objid,value);
    /// </summary>
    internal class SetLevelCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetLevelCommand cmd = new SetLevelCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int value = m_Value.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.Level = value;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_Value = new StoryValue<int>();
    }
    /// <summary>
    /// setattr(objid,attrid,value);
    /// </summary>
    internal class SetAttrCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetAttrCommand cmd = new SetAttrCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_AttrId = m_AttrId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_AttrId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);

        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int attrId = m_AttrId.Value;
            object value = m_Value.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                try {
                    long val = (long)Convert.ChangeType(value, typeof(long));
                    charObj.BaseProperty.SetLong((CharacterPropertyEnum)attrId, val);
                    charObj.ActualProperty.SetLong((CharacterPropertyEnum)attrId, val);
                } catch (Exception ex) {
                    LogSystem.Warn("setattr throw exception:{0}\n{1}", ex.Message, ex.StackTrace);
                }
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 2) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_AttrId.InitFromDsl(callData.GetParam(1));
                m_Value.InitFromDsl(callData.GetParam(2));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_AttrId = new StoryValue<int>();
        private IStoryValue<object> m_Value = new StoryValue();
    }
    /// <summary>
    /// markcontrolbystory(objid,true_or_false);
    /// </summary>
    internal class MarkControlByStoryCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            MarkControlByStoryCommand cmd = new MarkControlByStoryCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_Value = m_Value.Clone();
            return cmd;
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_Value.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            string value = m_Value.Value;
            EntityInfo charObj = PluginFramework.Instance.GetEntityById(objId);
            if (null != charObj) {
                charObj.IsControlByStory = (0 == value.CompareTo("true"));
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num >= 2) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_Value.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<string> m_Value = new StoryValue<string>();
    }
    /// <summary>
    /// setunitid(obj_id, dir);
    /// </summary>
    internal class SetUnitIdCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            SetUnitIdCommand cmd = new SetUnitIdCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_UnitId = m_UnitId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_UnitId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int unitId = m_UnitId.Value;
            EntityInfo obj = PluginFramework.Instance.GetEntityById(objId);
            if (null != obj) {
                obj.SetUnitId(unitId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_UnitId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_UnitId = new StoryValue<int>();
    }
    /// <summary>
    /// objsetcamp(objid,camp_id);
    /// </summary>
    internal class ObjSetCampCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetCampCommand cmd = new ObjSetCampCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_CampId = m_CampId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_CampId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            EntityInfo obj = PluginFramework.Instance.GetEntityById(m_ObjId.Value);
            if (null != obj) {
                int campId = m_CampId.Value;
                obj.SetCampId(campId);
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_CampId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_CampId = new StoryValue<int>();
    }
    /// objsetsummonerid(objid, objid);
    /// </summary>
    internal class ObjSetSummonerIdCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetSummonerIdCommand cmd = new ObjSetSummonerIdCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_SummonerId = m_SummonerId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_SummonerId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int summonerId = m_SummonerId.Value;
            EntityInfo npcInfo = PluginFramework.Instance.GetEntityById(objId);
            if (null != npcInfo) {
                npcInfo.SummonerId = summonerId;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_SummonerId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SummonerId = new StoryValue<int>();
    }
    /// objsetsummonskillid(objid, objid);
    /// </summary>
    internal class ObjSetSummonSkillIdCommand : AbstractStoryCommand
    {
        public override IStoryCommand Clone()
        {
            ObjSetSummonSkillIdCommand cmd = new ObjSetSummonSkillIdCommand();
            cmd.m_ObjId = m_ObjId.Clone();
            cmd.m_SummonSkillId = m_SummonSkillId.Clone();
            return cmd;
        }

        protected override void ResetState()
        {
        }

        protected override void Evaluate(StoryInstance instance, object iterator, object[] args)
        {
            m_ObjId.Evaluate(instance, iterator, args);
            m_SummonSkillId.Evaluate(instance, iterator, args);
        }

        protected override bool ExecCommand(StoryInstance instance, long delta)
        {
            int objId = m_ObjId.Value;
            int summonSkillId = m_SummonSkillId.Value;
            EntityInfo npcInfo = PluginFramework.Instance.GetEntityById(objId);
            if (null != npcInfo) {
                npcInfo.SummonSkillId = summonSkillId;
            }
            return false;
        }

        protected override void Load(Dsl.CallData callData)
        {
            int num = callData.GetParamNum();
            if (num > 1) {
                m_ObjId.InitFromDsl(callData.GetParam(0));
                m_SummonSkillId.InitFromDsl(callData.GetParam(1));
            }
        }

        private IStoryValue<int> m_ObjId = new StoryValue<int>();
        private IStoryValue<int> m_SummonSkillId = new StoryValue<int>();
    }
}
