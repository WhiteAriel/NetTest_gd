# -*- coding: utf-8 -*-
"""
Created on Fri Apr 22 16:18:56 2016

@author: Administrator
"""

import urllib2
import json
import re
import urllib


def entrance(url):   
    matches=parseUrl(url)
    downloadList=[]
    for item in matches:
        downloadList.append(item.rstrip())#rstrip,从右删除字符串末尾的指定字符（默认为空格）；append是向downloadList尾部添加对象
    if  downloadList==[]:
        #print 'None'
        return None
    elif downloadList!=[]:
        #print downloadList
        return downloadList[0] 
 

    
       
def parseUrl(videoUrl):
    targetUrl=compileUrl(videoUrl)#targetUrl为播放页面在硕鼠搜索后的页面地址
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers(targetUrl)
    )
    resp=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
    content=resp.read()   
   
    patternReal=judgeRealUrl(videoUrl)
    return  patternReal.findall(content) 

            

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


def judgeIeUrl(ieUrl):         #用来抓取视频网站首页的模式
    if 'sohu' in ieUrl:        #sohu匹配模式
        return re.compile(r'<a href=\"(http://tv\.sohu\.com.+?\.shtml)')
    elif 'ifeng' in ieUrl:    #fenghuan匹配模式
        return re.compile(r'<a href=\"(http://v\.ifeng\.com/program/.+?\.shtml)')
    elif 'sina' in ieUrl:       #sina匹配模式
        return re.compile(r'<a href=\"(http://video\.sina\.com\.cn/view/.+?\.html)')
    elif '163'  in ieUrl:       #163匹配模式
        return re.compile(r'<a title=\".*?\" href=\"(http://v\.163\.com/special/.+?\.html)')
 
def judgeRealUrl(ieUrl):       #用来抓取视频真实地址的模式
    if 'sohu' in ieUrl:        #sohu匹配模式
        return re.compile(r'<a href=\"(http://data.vod.itc.cn[^\"]+)')
    elif 'ifeng' in ieUrl:    #fenghuan匹配模式
        return re.compile(r'<a href=\"(http://ips.ifeng.com[^\"]+)')
    elif 'sina' in ieUrl:       #sina匹配模式
        return re.compile(r'<a href=\"(http://edge.ivideo.sina.com.cn[^\"]+)')
    elif '163'  in ieUrl:       #163匹配模式
        return re.compile(r'<a href=\"(http://flv4.bn.netease.com[^\"]+)')
        
def replaceStr(str):
    return str.replace('/','').replace(':','').replace('\?','').replace('\\','').replace('\*','').replace('\"','').replace('<','').replace('>','').replace('\|','')


def getrealUrl(urlList): 
    print urlList     
    for url in urlList:             #遍历列表中所有的视频网站
        response=urllib2.urlopen(url)
        html=response.read()
        ieList=[]
        realList=[]
        realCount=0
        patternIe=judgeIeUrl(url)     #根据视频网站不同返回不同的pattern
        ieList=patternIe.findall(html)
        #print ieList
        blist = set(ieList)       #去重
        #print blist
        for item in blist:                     #遍历每个视频网站的视频ie地址，每个有效的地址取第一个，取10个地址
            if entrance(item)!=None:
               realList.append(entrance(item))
            realCount+=1
            if realCount>4:
                break
        #print realList
        url=replaceStr(url)+r'.txt'
        with open(url,'w') as fs:
            fs.write(json.dumps(realList))       #不同网站的真实地址写入不同的文件
    #print ieList
    #with open(r'ie.txt','w') as fs:
        #fs.write(json.dumps(ieList))
        
        
def getflvcdvedio():
    targetUrl='http://www.flvcd.com/'
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers('')
    )
    response=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
    content=response.read()
    #content=content
    linkList1=[]
    nameList2=[]
    tmp=r'<a href="http://www.flvcd.com/redirect.php?url=http://tv.sohu.com'
    if tmp in content:
        linkList1.append('http://tv.sohu.com')
    tmp=r'<a href="http://www.flvcd.com/redirect.php?url=http://www.letv.com'
    #if tmp in content:
        #linkList1.append('http://www.letv.com')
    tmp=r'<a href="http://www.flvcd.com/redirect.php?url=http://v.163.com/'
    if tmp in content:
        linkList1.append('http://v.163.com/')
    tmp=r'<a href="http://www.flvcd.com/redirect.php?url=http://video.sina.com.cn/index.shtml'
    if tmp in content:
        linkList1.append('http://video.sina.com.cn/index.shtml')
    tmp=r'<a href="http://www.flvcd.com/redirect.php?url=http://v.ifeng.com/'
    if tmp in content:
        linkList1.append('http://v.ifeng.com/')       
    #links=re.compile(r'<a href=\"http://www.flvcd.com/redirect.php\?url=(http://tv.sohu.com[^\"]+)')    #网站链接links
    #matchesLinks=links.findall(content)
    #print   matchesLinks  
    #if matchesLinks:
        #linkList1.append(r'http://tv.sohu.com')
        #s='搜狐'
        #nameList2.append(s.decode('utf-8'))
    #names=re.compile(r'<a href=\"http://www.flvcd.com/redirect.php[^>]*?>([^<]+?)</a>')      #网站名称
    #pattern1=re.compile(r'<a href=\"(http://www.flvcd.com/redirect.php[^\"]+)')    #遇到“停止匹配 
    
    #matchesNames=names.findall(content)

    #j=0
    #for item in matchesNames:
        #j=j+1
        #nameList2.append(item.decode('gbk').encode('utf-8'))
        #if j>20:
           #break
    #with open(r'names.txt','w') as fs:
        #fs.write(json.dumps(nameList2,ensure_ascii=False,indent=2))      #输出是网站名字
    #i=0
    #for item in matchesLinks:
        #i=i+1
        #linkList1.append(item.rstrip())   #删除字符串末尾的空白符
        #if i>20:
            #break
    with open(r'links.txt','w') as fs:
        fs.write(json.dumps(linkList1,ensure_ascii=False,indent=2))      #输出链接  
    getrealUrl(linkList1)  

    
def testUrl(url):
    response=urllib2.urlopen(url)
    html=response.read()
    print html
    #print url
    #print isinstance(url,str)
    #response=urllib2.urlopen(url)
    #html=response.read()
    #print html   
    
"""   """   
if __name__=='__main__':#这个表示执行的是此代码所在的文件。如果这个文件是作为模块被其他文件调用，不会执行这里面的代码。只有执行这个文件时，if里面的语句才会被执行。这个功能经常可以用于进行测试。
   #url='http://v.ifeng.com/fhlbt/special/20160412/index.shtml#01ed43c7-32f4-4db2-8768-3367c63c679c';
   #entrance(url)
   #List=[]
   #List.append('http://tv.sohu.com')
   #List.append('http://v.ifeng.com/')
   #List.append('http://video.sina.com.cn/')
   #List.append('http://v.163.com/')
   #getrealUrl(List)
   List=getflvcdvedio()
   #testUrl(List[0])
   getrealUrl(List)
   #url='http://www.56.com'
   #testUrl(url)
   #print replaceStr(url)
   #entrance('http://video.sina.com.cn/view/250562666.html')