using System;
using System.Collections.Generic;
using MonsterAction.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterAction.Home.UI
{
    
    public class EquipmentMenu : HomeMenuBase
    {
        [Serializable]
        public struct PlayerTypeOption
        {
            [Tooltip("BattlePlayerSpawner の typeId と同じにする")]
            public string id;
            public string displayName;
            [TextArea] public string description;
        }

        private static readonly Color OnColor = new Color(0.45f, 1f, 0.5f);
        private static readonly Color OffColor = new Color(0.75f, 0.75f, 0.75f);
        private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f);

        [SerializeField]
        private PlayerTypeOption[] playerTypes =
        {
            new PlayerTypeOption { id = "SwordShield", displayName = "片手剣", description = "□ 連続攻撃 / □長押し ガード。\n攻守のバランスがよい。" },
            new PlayerTypeOption { id = "Bow", displayName = "弓", description = "□ 矢を撃つ / □長押し 溜め撃ち / L1 で狙う。\n離れて戦う。" },
            new PlayerTypeOption { id = "Magic", displayName = "魔法", description = "□ 魔法の弾を撃つ / □長押し 溜めて周りに範囲攻撃 / L1 で狙う。\n離れて戦う。" },
        };

        [Tooltip("「スキル 1 / 2」のような枠の表示")]
        [SerializeField] private TextMeshProUGUI slotText;

        // 行の中身（タイプかスキルのどちらか）
        private struct Entry
        {
            public int typeIndex; // タイプの番号（playerTypes の何番目か）。-1 ならスキルの行
            public SkillType skill;
            public bool IsType => typeIndex >= 0;
        }

        // rows[0] の中身は entries[0]、rows[1] の中身は entries[1] … と、同じ番号で対応している
        private readonly List<Entry> entries = new List<Entry>();

        /// <summary>タイプ ID から表示名を返す（出撃画面などで使う）</summary>
        public string GetPlayerTypeName(string id)
        {
            foreach (var t in playerTypes)
                if (t.id == id) return t.displayName;
            return id;
        }

        // ================================================================
        //  一覧を作る（メニューを開いたときに1回呼ばれる）
        // ================================================================

        protected override void BuildRows()
        {
            entries.Clear();

            CreateHeader("プレイヤータイプ");
            for (int i = 0; i < playerTypes.Length; i++)
            {
                CreateRow();                               // 行を1つ作る
                entries.Add(new Entry { typeIndex = i });  // その行の中身（何番目のタイプか）を覚えておく
            }

            // TODO ②：スキルも並べる
            CreateHeader("スキルタイプ");
            foreach (SkillType skill in SkillData.All)
            {
                CreateRow(); //行を作る
                entries.Add(new Entry { typeIndex = -1, skill = skill });
            }

            //SkillData.All を foreach して、CreateRow() と entries.Add(new Entry { typeIndex = -1, skill = skill })
            //   （typeIndex = -1 で「スキルの行」という意味になります）
        }

        // ================================================================
        //  表示を最新にする（開いたとき・付け外ししたあとに呼ばれる）
        // ================================================================

        protected override void Refresh()
        {
            if (moneyText != null) moneyText.text = "所持金  " + FormatMoney(GameData.Money);
            if (slotText != null) slotText.text = $"スキル枠  {GameData.EquippedSkillCount} / {GameData.MaxEquippedSkills}";

            for (int i = 0; i < rows.Count && i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.IsType)
                {
                    // タイプの行：「片手剣」「● 選択中」
                    var t = playerTypes[e.typeIndex];
                    bool selected = GameData.SelectedPlayerTypeId == t.id;
                    rows[i].Set(t.displayName, "", selected ? "● 選択中" : "", OnColor);
                }
                else
                {
                    // TODO ③：スキルの行の表示
                    //   ・int level = GameData.GetSkillLevel(e.skill);  で今の★の数（0 なら未購入）
                    //   ・未購入なら … rows[i].Set(名前, SkillData.GetStars(0), "未購入", DisabledColor);
                    //   ・買ってあれば、GameData.IsSkillEquipped(e.skill) で付けているか調べて
                    //       付けている → 右に "装備中"（OnColor）
                    //       外している → 右に "外している"（OffColor）
                    //   ・名前は SkillData.GetName(e.skill)、情報は SkillData.GetStars(level)
                    string skillName = SkillData.GetName(e.skill);
                    int level = GameData.GetSkillLevel(e.skill);
                    if (level <= 0) //レベル0＝未購入
                    {
                        rows[i].Set(skillName, SkillData.GetStars(0), "未購入", DisabledColor);//rowは行、上からi番目
                    }
                    else if(GameData.IsSkillEquipped(e.skill)) //GameDataにスキルを装備しているか質問している
                    {
                        rows[i].Set(skillName, SkillData.GetStars(level), "装備中", OnColor);
                    }
                    else
                    {
                        rows[i].Set(skillName, SkillData.GetStars(level), "外している",OffColor);
                    }
                }
            }
        }

        // ================================================================
        //  行にカーソルが乗った → 説明を出す
        // ================================================================

        protected override void OnRowSelected(int index) //行が選択された時
        {
            if (index < 0 || index >= entries.Count) return;　//もしindexの値が0以下、規定以上の場合はreturnする（エラー防止）||はor
            var e = entries[index];　

            if (e.IsType) //これでプレイヤータイプの説明を出している
            {
                SetDescription(playerTypes[e.typeIndex].description);
            }
            else　//それ以外はスキルの説明になる
            {
                // TODO ④：スキルの説明を出す
                //   SkillData.GetDescription(e.skill) を SetDescription に渡す
                //   （未購入なら「村人のショップで買えます。」と付け足すと親切）
                SetDescription(SkillData.GetDescription(e.skill));
                if(GameData.GetSkillLevel(e.skill)<=0)
                {
                    SetDescription("村人のショップで買えます");
                }
            }
        }

        // ================================================================
        //  行が決定された → タイプを選ぶ / スキルを付け外しする
        // ================================================================

        protected override void OnRowClicked(int index)　//行がクリックされた時
        {
            if (index < 0 || index >= entries.Count) return;
            var e = entries[index];

            if (e.IsType)
            {
                // TODO ①：そのタイプにする
                var t = playerTypes[e.typeIndex];  //で記憶させていたタイプを取り出す
                GameData.SelectedPlayerTypeId = t.id; //GameDataにidを入れる
                ShowMessage($"{t.displayName} にした"); //メッセージを表示させる

                

            }
            else
            {

                // TODO ⑤：スキルを付ける / 外す
                //   1. 未購入（GameData.GetSkillLevel が 0 以下）なら … "まだ買っていません"
                //   2. 付けていれば外す、外していれば付ける（GameData.SetSkillEquipped）
                //   3. SetSkillEquipped が false なら（枠がいっぱい）… "スキル枠がいっぱいです。先にどれかを外してください"
                if(GameData.GetSkillLevel(e.skill)<=0)//クリックした行のスキルレベルが0以下
                {
                    ShowMessage("まだ買っていません");
                }
                else if(GameData.IsSkillEquipped(e.skill)) //もしスキルを装備していたら
                {
                    GameData.SetSkillEquipped(e.skill, false);//falseで外す
                }
                else if (GameData.EquippedSkillCount >= GameData.MaxEquippedSkills)//制限より多くスキルをつけようとしたら
                {
                    ShowMessage("スキル枠がいっぱいです。先にどれかを外してください");
                }
                else
                {
                    GameData.SetSkillEquipped(e.skill, true);//true付ける
                }
                

            }

            Refresh(); // 表示を更新する
        }
    }
}
