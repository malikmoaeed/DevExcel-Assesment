document.querySelectorAll('[data-product-form]').forEach(form => {
 form.addEventListener('submit', event => {
  const active = form.querySelector('#IsActive');
  if (active && !active.checked && form.dataset.wasActive === 'true' && !window.confirm('Deactivate this product? History will be preserved, but new stock movements will be blocked.')) event.preventDefault();
 });
});
