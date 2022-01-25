# -*- coding: utf-8 -*-
import sys
import os
import json
import sqlite3

path=sys.argv[1]
types=["en-US","ja-JP","zh-CN"]
data={}
for _type in types:
    db=sqlite3.connect(path+'\\'+_type+"\\cards.cdb")
    cursor=db.cursor()
    cursor.execute("select id,name,desc from texts;")
    for id,name,desc in cursor.fetchall():
        if id not in data:
            data[id]={}
            data[id]["en"]=""
            data[id]["ja"]=""
            data[id]["zh"]=""
        _data=data[id]
        _data[_type[0:2]]=name
        _data["desc"]=desc
with open('data.json', 'w', encoding='utf-8') as f:
    json.dump(data, f, ensure_ascii=False, indent=4)
os.system("pause")