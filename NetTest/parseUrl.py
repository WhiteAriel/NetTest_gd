# -*- coding: utf-8 -*-
"""
Created on Sat Apr 09 17:20:16 2016
@author: Administrator
"""

import re
import json
import urllib
import urllib2


def entrance(url):   
    matches=parseUrl(url)
    downloadList=[]
    for item in matches:
        downloadList.append(item.rstrip())#rstrip,从右删除字符串末尾的指定字符（默认为空格）；append是向downloadList尾部添加对象
    if  downloadList==[]:
        return 0
    elif downloadList!=[]:
        print downloadList 
        with open(r'test.txt','w') as fs:
             fs.write(json.dumps(downloadList))   
        return 1 

#以下为各网站播放页面，我们将会用输入网址和它们匹配        
#def youku_v_show():
    #return re.compile(r'http://v\.youku\.com/v_show/id_.+\.html')  #播放页面      
def ku6_v_show():
    return re.compile(r'http://v\.ku6\.com.+\.html')
def sina_v_show():
    return re.compile(r'http://.+\.sina\.com\.cn.+\.html')
def netease_v_show():
    return re.compile(r'http://v\.163\.com.+\.html')
def ifeng_v_show():
    return re.compile(r'http://v\.ifeng\.com.+\.shtml')
def mgtv_v_show():
    return re.compile(r'http://www\.mgtv\.com/v.+\.html')
def sohutv_v_show():
    return re.compile(r'http://.+\.sohu\.com.+\.shtml') 
def cctv_v_show():
    return re.compile(r'http://tv\.cctv\.com.+\.shtml')    
def zjstv_v_show():
    return re.compile(r'http://v\.zjstv\.com.+\.html') 
    
        
def parseUrl(videoUrl):
    #youku_vshow=youku_v_show()        #play url
    ku6_vshow=ku6_v_show()
    sina_vshow=sina_v_show()
    netease_vshow=netease_v_show()
    ifeng_vshow=ifeng_v_show()
    mgtv_vshow=mgtv_v_show()
    sohutv_vshow=sohutv_v_show()
    cctv_vshow=cctv_v_show()
    zjstv_vshow=zjstv_v_show()
    
    
    targetUrl=compileUrl(videoUrl)#targetUrl为播放页面在硕鼠搜索后的页面地址
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers(targetUrl)
    )
    resp=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
    content=resp.read()   
    #if youku_vshow.match(videoUrl):# 播放页面和优酷（自己输入）的页面，判断是否是视频页面
       #def youku_herfFileUrl():
          #return re.compile(r'<a href=\"(http://k.youku.com/player/getFlvPath[^\"]+)') 
       #youku_herfshow=youku_herfFileUrl()
       #return youku_herfshow.findall(content)#re正则中findall以列表的形式返回能匹配的子串，列表里面是匹配的结果形成的元组形式
       
    if ku6_vshow.match(videoUrl):
         def ku6_herfFileUrl():
             return re.compile(r'<a href=\"(http://main.gslb.ku6.com[^\"]+)')
         ku6_herfshow=ku6_herfFileUrl()    #patern
         return ku6_herfshow.findall(content) 
         
    elif sina_vshow.match(videoUrl):
         def sina_herfFileUrl():
             return re.compile(r'<a href=\"(http://edge.ivideo.sina.com.cn[^\"]+)')
         sina_herfshow=sina_herfFileUrl() 
         return sina_herfshow.findall(content) 
       
    elif netease_vshow.match(videoUrl):
         def netease_herfFileUrl():
             return re.compile(r'<a href=\"(http://flv4.bn.netease.com[^\"]+)')
         netease_herfshow=netease_herfFileUrl()
         return netease_herfshow.findall(content)
       
    elif ifeng_vshow.match(videoUrl):
         def ifeng_herfFileUrl():
             return re.compile(r'<a href=\"(http://ips.ifeng.com[^\"]+)')
         ifeng_herfshow=ifeng_herfFileUrl()  
         return ifeng_herfshow.findall(content) 
       
    elif mgtv_vshow.match(videoUrl):
         def mgtv_herfFileUrl():
             return re.compile(r'<a href=\"(http://disp.titan.mgtv.com[^\"]+)')
         mgtv_herfshow=mgtv_herfFileUrl()
         return mgtv_herfshow.findall(content)
       
    elif sohutv_vshow.match(videoUrl):
         def sohutv_herfFileUrl():
             return re.compile(r'<a href=\"(http://data.vod.itc.cn[^\"]+)') 
         sohutv_herfshow=sohutv_herfFileUrl()
         return sohutv_herfshow.findall(content) 
       
    elif cctv_vshow.match(videoUrl):
         def cctv_herfFileUrl():
             return re.compile(r'<a href=\"(http://vod.cntv.lxdns.com[^\"]+)')    #[^\"]+一直匹配到“之外的字符
         cctv_herfshow=cctv_herfFileUrl()
         return cctv_herfshow.findall(content) 
       
    elif zjstv_vshow.match(videoUrl):
         def zjstv_herfFileUrl():
             return re.compile(r'<a href=\"(http://play.g3proxy.lecloud.com[^\"]+)') 
         zjstv_herfshow=zjstv_herfFileUrl()
         return zjstv_herfshow.findall(content)
       
    #else:
        #return 0#输入错误
            

def headers(url):
    heads={                      
        'Host':'www.flvcd.com',#硕鼠网址
        'Referer':url,
        'User-Agent':'Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 '
    }        
    return heads 
    
def compileUrl(videoUrl):
    Bae_Url="http://www.flvcd.com/parse.php?format=&"#经过硕鼠处理的网页前半部分        
    params={'kw':videoUrl}
    encode=urllib.urlencode(params)#对URL字符串编码
    realUrl="%s%s"%(Bae_Url,encode)
    return realUrl
   #以上获取硕鼠页面地址 

def getflvcdvedio():
    targetUrl='http://www.flvcd.com/'
    print targetUrl
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers('')
    )
    response=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
    #print response.encoding
    content=response.read()
    content=content.decode('gbk')
    #print content
    #print str(content) 
    #print str(content).decode('utf-8') 
    #print str(content).encode('utf-8') 
    #print str(content).decode('gbk') 
    #print str(content).encode('gbk') 
    #print content.decode('utf-8') 
    #print content.encode('utf-8') 
    #print content.decode('gbk') 
    #print content.encode('gbk')
    #content = unicode(content, "gb2312").encode("utf8")
    print content
    #response1=urllib2.urlopen('http://www.flvcd.com/')
    #html1=response1.read()
    pattern1=re.compile(r'<a href=\"http://www.flvcd.com/redirect.php\?url=(http://[^\"]+)')
    pattern2=re.compile(r'<a href=\"http://www.flvcd.com/redirect.php[^>]*?>([^<]+?)</a>')
    #pattern1=re.compile(r'<a href=\"(http://www.flvcd.com/redirect.php[^\"]+)')    #遇到“停止匹配 
    matches1=pattern1.findall(content)
    matches2=pattern2.findall(content)
    downloadList1=[]
    downloadList2=[]
    i=0
    for item in matches1:
        i=i+1
        downloadList1.append(item.rstrip())   #删除字符串末尾的空白符
        if i>10:
            break
    print downloadList1
    j=0
    for item in matches2:
        j=j+1
        print isinstance(item,unicode)
        print item
        downloadList2.append(item.encode('utf-8'))
        if j>10:
           break
    with open(r'test1.txt','w') as fs:
        fs.write(json.dumps(downloadList2,ensure_ascii=False,indent=2))
    #print downloadList2
    
"""   """   
if __name__=='__main__':#这个表示执行的是此代码所在的文件。如果这个文件是作为模块被其他文件调用，不会执行这里面的代码。只有执行这个文件时，if里面的语句才会被执行。这个功能经常可以用于进行测试。
   #url='http://v.ifeng.com/fhlbt/special/20160412/index.shtml#01ed43c7-32f4-4db2-8768-3367c63c679c';
   #entrance(url)
   getflvcdvedio()