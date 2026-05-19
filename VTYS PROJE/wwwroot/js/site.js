// Telefon: sadece rakam, en fazla 11 hane
document.querySelectorAll('[data-input="phone"]').forEach((input) => {
    input.addEventListener('input', () => {
        input.value = input.value.replace(/\D/g, '').slice(0, 11);
    });
});

// Harf alanlari: rakam ve ozel karakter engelle
document.querySelectorAll('[data-input="letters"]').forEach((input) => {
    input.addEventListener('input', () => {
        input.value = input.value.replace(/[^a-zA-ZçÇğĞıİöÖşŞüÜ\s'-]/g, '');
    });
});
