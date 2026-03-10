
document.getElementById('rewrite').onclick=async()=>{
 const text=document.getElementById('input').value;
 const res=await fetch('http://localhost:5000/api/ai/rewrite',{
   method:'POST',
   headers:{'Content-Type':'application/json'},
   body:JSON.stringify({message:text})
 });
 const data=await res.json().catch(()=>({}));
 document.getElementById('output').textContent=JSON.stringify(data,null,2);
};
