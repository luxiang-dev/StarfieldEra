namespace StarfieldEra
{
    public static class BuildingEconomy
    {
        public static readonly string[] Effects={
            "每人每日原料4/6/8；二级加工每人2份，需派驻村民。",
            "每人每日原料4/6/8；二级加工每人2份，需派驻村民。",
            "每天知识+3。","每天料理+2；走近可制作。",
            "村民容量+2（上限9）；可休息。","各资源点库存+30（上限120）。",
            "每级每天木材+6；派驻村民加工木板，二级解锁石砖；解锁书院与工具。",
            "每天知识+6；需工坊，解锁灌溉与观星台。",
            "每天金币+18；需工坊。",
            "每天知识+12；需书院、击败白岩守卫，另耗晶石3、知识20。",
            "每天金币+10；走近可交易。","每天胡萝卜+3；走近可休息、料理。"
        };
        public static string Description(int kind)=>kind>=0&&kind<Effects.Length?Effects[kind]:"";
    }
}
