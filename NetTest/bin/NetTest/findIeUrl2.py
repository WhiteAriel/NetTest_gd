# -*- coding: utf-8 -*-
"""
Created on Fri Apr 22 16:18:56 2016

@author: Administrator
"""

import urllib2
import json
import re
import urllib
import threading



def entrance(url):   
    matches=parseUrl(url)
    if matches=='0':
        return 0
    downloadList=[]
    for item in matches:
        downloadList.append(item.rstrip())#rstrip,从右删除字符串末尾的指定字符（默认为空格）；append是向downloadList尾部添加对象
    if  downloadList==[]:
        #print 'None'
        return 0
    elif downloadList!=[]:
        #print downloadList[0]
        return downloadList[0] 
        
def entrance2(url):   
    matches=parseUrl(url)
    if matches=='0':
        return 0
    downloadList=[]
    for item in matches:
        downloadList.append(item.rstrip())#rstrip,从右删除字符串末尾的指定字符（默认为空格）；append是向downloadList尾部添加对象
    if  downloadList==[]:
        #print 'None'
        return 0
    elif downloadList!=[]:
        #print downloadList[0]
        with open(r'test.txt','w') as fs:
            fs.write(json.dumps(downloadList))
        return 1 
    
       
def parseUrl(videoUrl):
    try:
        targetUrl=compileUrl(videoUrl)#targetUrl为播放页面在硕鼠搜索后的页面地址
        req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
            url =targetUrl,
            headers =headers(targetUrl)
        )
        resp=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
        content=resp.read()   
       
        patternReal=judgeRealUrl(videoUrl)
        return  patternReal.findall(content) 
    except Exception , e:
        #print 'parseUrl'
        #print e
        return '0'

            

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
        return re.compile(r'<a href=\"(http://tv\.sohu\.com/2016.+?\.shtml)')
    elif 'ifeng' in ieUrl:    #fenghuan匹配模式
        return re.compile(r'<a href=\"(http://v\.ifeng\.com/news/.+?\.shtml)')
    elif 'sina' in ieUrl:       #sina匹配模式
        return re.compile(r'<a href=\"(http://video\.sina\.com\.cn.+?\.html)')
    elif '163'  in ieUrl:       #163匹配模式
        return re.compile(r'<a title=\".*?\" href=\"(http://v\.163\.com/paike/.+?\.html)')
 
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
    #print urlList
    try: 
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
                if entrance(item)!=0:
                    #if entrance(item)==:
                        #print 'exc'
                    realList.append(entrance(item))
                    realCount+=1
                    if realCount>4:
                       break
            #print realList
            url=replaceStr(url)+r'.txt'
            with open(url,'w') as fs:
                fs.write(json.dumps(realList))       #不同网站的真实地址写入不同的文件
            return 1
    except Exception , e:
        #print e
        return 0
    #print ieList
    #with open(r'ie.txt','w') as fs:
        #fs.write(json.dumps(ieList))

def parseName(url):
    if 'sohu' in url:        #sohu匹配模式
        return 'sohu'
    elif 'ifeng' in url:    #fenghuan匹配模式
        return 'ifeng'
    elif 'sina' in url:       #sina匹配模式
        return 'sina'
    elif '163'  in url:       #163匹配模式
        return 'netbase' 

def getrealUrl2(url):
    try:
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
            if entrance(item)!=0:
               realList.append(entrance(item))
               realCount+=1
               if realCount>4:
                   break
        #print realList
        url=parseName(url)+r'.txt'
        with open(url,'w') as fs:
            fs.write(json.dumps(realList))       #不同网站的真实地址写入不同的文件
        return 1
    except Exception , e:
        #print 'getrealUrl2'
        #print e
        return 0    

      
        
def getflvcdvedio2():
    targetUrl='http://www.flvcd.com/'
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers('')
    )
    try:
        response=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
        content=response.read()
        #print content
        linkList1=[]
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
        with open(r'links.txt','w') as fs:
            fs.write(json.dumps(linkList1,ensure_ascii=False,indent=2))      #输出链接  
        #getrealUrl(linkList1)
        #while True:
        threads=[]
        nloops=range(len(linkList1))
        for item in linkList1:
            #print item
            t=threading.Thread(target=getrealUrl2,args=(item,))
            threads.append(t)
        for i in nloops:
            threads[i].start()
            
        for i in nloops:
            threads[i].join()
        del threads[:]
        #print '---------------all DONE----------- '
        #sleep(5)   
        return 1
    except Exception , e:
        #print 'getflvcdvedio'
        #print e
        return 0
  
  
def getflvcdvedio():       #判断哪些网站可以用
    targetUrl='http://www.flvcd.com/'
    req=urllib2.Request(#建立了一个Request对象来明确指明想要获取的url
        url =targetUrl,
        headers =headers('')
    )
    try:
        response=urllib2.urlopen(req)#抓取硕鼠搜索页面的内容
        content=response.read()
        #print content
        linkList1=[]
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
        with open(r'links.txt','w') as fs:
            fs.write(json.dumps(linkList1,ensure_ascii=False,indent=2))      #输出链接 
            return 1
    except Exception , e:
        return 0

    
def testUrl(url):
    response=urllib2.urlopen(url)
    html=response.read()
    print html
    #print url
    #print isinstance(url,str)
    #response=urllib2.urlopen(url)
    #html=response.read()
    #print html 
    
def testJson():
    tests=[]
    tests.append(1)
    tests.append(2)
    tests.append(None)
    with open(r'json.txt','w') as fs:
            fs.write(json.dumps(tests,ensure_ascii=False,indent=2))      #输出链接  
    
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
   #if getflvcdvedio2()==0:
       #print '获取异常!!'
   #testJson()
   #testUrl(List[0])
   #getrealUrl(List)
   #url='http://www.56.com'
   #testUrl(url)
   #print replaceStr(url)
   #entrance('http://video.sina.com.cn/view/250562666.html')
   getrealUrl2('http://v.ifeng.com/')