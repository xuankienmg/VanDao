const fs=require('fs');const p='wwwroot/js/market.js';let s=fs.readFileSync(p,'utf8');s=s.replace('<h3>Hành trang <small>${own.length} loại vật phẩm</small></h3>','<h3>Hành trang <small>${own.length} loại vật phẩm</small></h3><label class="mk-bag-order">Sắp xếp <select id="mkBagSort">${MK_BAG_SORTS.map(([k,n])=>`<option value="${k}" ${k===mkBagSort?\'selected\':\'\'}>${n}</option>`).join(\'\')}</select></label>');s=s.replace("pane.addEventListener('change', e => {",`pane.addEventListener('change', e => {
    if (e.target.id === 'mkBagSort'){
      const qty = $('mkQty')?.value, price = $('mkPrice')?.value;
      mkBagSort = e.target.value; renderMarket();
      if ($('mkQty') && qty !== undefined) $('mkQty').value = qty;
      if ($('mkPrice') && price !== undefined) $('mkPrice').value = price;
      mkNet();
    }`);fs.writeFileSync(p,s);
